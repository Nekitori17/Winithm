using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Behaviors;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Global NoteManager: manages note spawning, positioning, and lifecycle
  /// for ALL windows from a single instance. Notes are keyed by windowId.
  /// One shared object pool, one _Process loop.
  /// </summary>
  public class NoteManager : Node
  {
    public event Action<string, NoteData> OnNoteHit;   // windowId, note
    public event Action<string, NoteData> OnNoteMiss;  // windowId, note

    private TimeManager _timeManager;
    private PackedScene _noteScene;

    // Per-window data
    private class WindowNoteState
    {
      public WindowData Data;
      public Window Visual;
      public Dictionary<NoteSide, List<NoteData>> PendingNotes;
      public Dictionary<NoteSide, List<NoteData>> ActiveNotes;
    }

    private Dictionary<string, WindowNoteState> _windowStates = new Dictionary<string, WindowNoteState>();

    // Shared visuals: NoteData → Note visual (across all windows)
    private Dictionary<NoteData, Note> _noteVisuals = new Dictionary<NoteData, Note>();

    // Shared object pool
    private Stack<Note> _notePool = new Stack<Note>();

    /// <summary>How many beats ahead of StartTime a note should spawn.</summary>
    public float NoteSpeedBeats = 2f;

    public void Init(TimeManager tm)
    {
      _timeManager = tm;
      _noteScene = GD.Load<PackedScene>("res://Winithm.Core/Resources/Sprites/Note.tscn");
    }

    /// <summary>
    /// Registers a window and its notes for management.
    /// Called when a window becomes active.
    /// </summary>
    public void RegisterWindow(string windowId, WindowData wd, Window window)
    {
      if (_windowStates.ContainsKey(windowId)) return;

      _windowStates[windowId] = new WindowNoteState()
      {
        Data = wd,
        Visual = window,
        ActiveNotes = new   
      };
    }

    /// <summary>
    /// Unregisters a window and cleans up all its notes.
    /// Called when a window becomes inactive.
    /// </summary>
    public void UnregisterWindow(string windowId)
    {
      if (!_windowStates.TryGetValue(windowId, out var state)) return;

      foreach (var note in state.ActiveNotes.Values)
      {
        if (_noteVisuals.TryGetValue(note, out var visual))
        {
          ReturnToPool(visual);
          _noteVisuals.Remove(note);
        }
      }

      _windowStates.Remove(windowId);
    }

    public override void _ExitTree()
    {
      base._ExitTree();

      foreach (var visual in _noteVisuals.Values)
      {
        if (IsInstanceValid(visual)) visual.QueueFree();
      }
      _noteVisuals.Clear();

      while (_notePool.Count > 0)
      {
        var visual = _notePool.Pop();
        if (IsInstanceValid(visual)) visual.QueueFree();
      }
    }

    public override void _Process(float delta)
    {
      if (_timeManager == null) return;
      float currentBeat = _timeManager.CurrentBeat;

      foreach (var pair in _windowStates)
      {
        ProcessWindow(pair.Key, pair.Value, currentBeat);
      }
    }

    private void ProcessWindow(string windowId, WindowNoteState state, float currentBeat)
    {
      if (state.Visual == null) return;

      var pending = state.PendingNotes;
      var active = state.ActiveNotes;

      // 1. Spawn notes that entered the visible ahead-time
      while (pending.Count > 0 && currentBeat >= pending[0].Start.AbsoluteValue - NoteSpeedBeats)

      {
        SpawnNote(windowId, state, pending[0]);
        pending.RemoveAt(0);
      }

      // 2. Update positions
      Vector2 winSize = state.Visual.WindowBodySize;

      List<NoteData> toRemove = null;

      for (int i = 0; i < active.Count; i++)
      {
        NoteData data = active[i];
        if (!_noteVisuals.TryGetValue(data, out Note visual)) continue;

        bool pastJudgement = IsNotePastLimit(data, visual, winSize, currentBeat);

        if (pastJudgement)
        {
          if (toRemove == null) toRemove = new List<NoteData>();
          toRemove.Add(data);
        }
        else
        {
          UpdateNoteVisual(data, visual, currentBeat, winSize);

          if (IsNoteBeforeVisibleArea(data, currentBeat))
            break;
        }
      }

      // 3. Remove expired notes
      if (toRemove != null)
      {
        foreach (var data in toRemove)
        {
          if (_noteVisuals.TryGetValue(data, out var visual))
          {
            ReturnToPool(visual);
            _noteVisuals.Remove(data);
          }
          active.Remove(data);

          if (!data.IsEvaluated)
          {
            OnNoteMiss?.Invoke(windowId, data);
          }
        }
      }
    }

    // =============================================
    // Spawn & Pool
    // =============================================

    private void SpawnNote(string windowId, WindowNoteState state, NoteData data)
    {
      Note visual;
      if (_notePool.Count > 0)
      {
        visual = _notePool.Pop();
        visual.Visible = true;
      }
      else
      {
        visual = _noteScene.Instance<Note>();
      }

      data.IsEvaluated = false;

      // Z-ordering: Focus notes above Unfocus overlay, everything else below
      Control parent = (data.Type == NoteType.Focus)
        ? state.Visual.FocusNoteLayer
        : state.Visual.NoteLayer;

      if (visual.GetParent() != parent)
      {
        if (visual.GetParent() != null) visual.GetParent().RemoveChild(visual);
        parent.AddChild(visual);
      }
      // Newer notes drawn UNDER older notes (closer to judgement = on top)
      parent.MoveChild(visual, 0);

      state.ActiveNotes.Add(data);
      _noteVisuals[data] = visual;
    }

    private void ReturnToPool(Note visual)
    {
      visual.Visible = false;
      if (visual.GetParent() != null) visual.GetParent().RemoveChild(visual);
      _notePool.Push(visual);
    }

    // =============================================
    // Position & Lifecycle
    // =============================================

    private bool IsNoteBeforeVisibleArea(NoteData data, float currentBeat)
    {
      return currentBeat < data.Start.AbsoluteValue - NoteSpeedBeats;
    }

    private bool IsNotePastLimit(NoteData data, Note visual, Vector2 winSize, float currentBeat)
    {
      float endBeat = data.Start.AbsoluteValue + data.Length.AbsoluteValue;
      float noteH = Mathf.Min(winSize.x, winSize.y) * 0.025f;
      float limit = noteH * 3f;

      if (data.Type != NoteType.Hold)
      {
        float t = (currentBeat - data.Start.AbsoluteValue) / NoteSpeedBeats;
        if (t <= 0) return false;

        switch (data.Side)
        {
          case NoteSide.Bottom: return visual.RectPosition.y > winSize.y + limit;
          case NoteSide.Top: return visual.RectPosition.y < -limit;
          case NoteSide.Right: return visual.RectPosition.x > winSize.x + limit;
          case NoteSide.Left: return visual.RectPosition.x < -limit;
        }
        return false;
      }
      else
      {
        return currentBeat > endBeat + (limit / Mathf.Max(winSize.y, 1f)) * NoteSpeedBeats;
      }
    }

    private void UpdateNoteVisual(NoteData data, Note visual, float currentBeat, Vector2 winSize)
    {
      float noteH = Mathf.Min(winSize.x, winSize.y) * 0.025f;
      float laneW = winSize.x; // default for vertical sides

      // t: 0 = just spawned (far edge), 1 = hitting judgement line
      float t = (currentBeat - (data.Start.AbsoluteValue - NoteSpeedBeats)) / NoteSpeedBeats;
      t = Mathf.Clamp(t, 0f, 2f);

      bool isHold = data.Type == NoteType.Hold;
      float bodyH = 0f;

      if (isHold)
      {
        float beatsTotalBody = data.Length.AbsoluteValue;
        float travelDist;

        switch (data.Side)
        {
          case NoteSide.Bottom:
          case NoteSide.Top:
            travelDist = winSize.y;
            break;
          default:
            travelDist = winSize.x;
            break;
        }

        float pixelsPerBeat = travelDist / NoteSpeedBeats;
        bodyH = beatsTotalBody * pixelsPerBeat;

        if (t >= 1f)
        {
          float beatsElapsed = currentBeat - data.Start.AbsoluteValue;
          bodyH = Mathf.Max(0f, bodyH - beatsElapsed * pixelsPerBeat);
        }
      }

      // Configure Note visual properties
      switch (data.Side)
      {
        case NoteSide.Bottom:
        case NoteSide.Top:
          visual.LaneWidth = winSize.x;
          break;
        case NoteSide.Left:
        case NoteSide.Right:
          visual.LaneWidth = winSize.y;
          break;
      }

      visual.NoteSize = 1f;
      visual.PlayerAreaSize = winSize;
      visual.Type = data.Type;
      visual.BodyHeight = bodyH;

      // Position based on Side
      float headH = visual.NoteSize * Mathf.Min(winSize.x, winSize.y) * 0.025f;

      switch (data.Side)
      {
        case NoteSide.Bottom:
        {
          float judgeY = winSize.y - headH;
          float posY = t < 1f
            ? Mathf.Lerp(-headH - bodyH, judgeY, t)
            : judgeY - bodyH;
          visual.RectPosition = new Vector2(winSize.x / 2f, posY + headH);
          visual.RectRotation = 0f;
          break;
        }
        case NoteSide.Top:
        {
          float judgeY = headH;
          float posY = t < 1f
            ? Mathf.Lerp(winSize.y + bodyH + headH, judgeY, t)
            : judgeY;
          visual.RectPosition = new Vector2(winSize.x / 2f, posY);
          visual.RectRotation = 180f;
          break;
        }
        case NoteSide.Right:
        {
          float judgeX = winSize.x - headH;
          float posX = t < 1f
            ? Mathf.Lerp(-headH - bodyH, judgeX, t)
            : judgeX - bodyH;
          visual.RectPosition = new Vector2(posX + headH, winSize.y / 2f);
          visual.RectRotation = -90f;
          break;
        }
        case NoteSide.Left:
        {
          float judgeX = headH;
          float posX = t < 1f
            ? Mathf.Lerp(winSize.x + bodyH + headH, judgeX, t)
            : judgeX;
          visual.RectPosition = new Vector2(posX, winSize.y / 2f);
          visual.RectRotation = 90f;
          break;
        }
      }

      visual.UpdateVisual();
    }

    // =============================================
    // HitManager API (called externally)
    // =============================================

    /// <summary>
    /// Finds all active notes of the given type across ALL windows within the hit window.
    /// Returns (windowId, note, offsetMs) tuples. Used for Focus/Close broadcast.
    /// </summary>
    public List<(string WindowId, NoteData Note, float OffsetMs)> TryEvaluateAll(NoteType type, float currentBeat, float maxWindowMs)
    {
      var results = new List<(string, NoteData, float)>();

      foreach (var pair in _windowStates)
      {
        string windowId = pair.Key;
        var active = pair.Value.ActiveNotes;

        foreach (var data in active)
        {
          if (data.IsEvaluated) continue;
          if (data.Type != type) continue;

          float offsetMs = BeatToMs(data.Start.AbsoluteValue - currentBeat);
          if (Math.Abs(offsetMs) > maxWindowMs) continue;

          results.Add((windowId, data, offsetMs));
        }
      }

      return results;
    }

    /// <summary>
    /// Finds the best Drag note across ALL windows within 100ms.
    /// </summary>
    public (string WindowId, NoteData Note, float OffsetMs)? TryEvaluateDrag(float currentBeat)
    {
      string bestWindowId = null;
      NoteData bestDrag = null;
      float bestAbsMs = 100f;

      foreach (var pair in _windowStates)
      {
        string windowId = pair.Key;
        foreach (var data in pair.Value.ActiveNotes)
        {
          if (data.IsEvaluated) continue;
          if (data.Type != NoteType.Drag) continue;

          float offsetMs = BeatToMs(data.Start.AbsoluteValue - currentBeat);
          float absMs = Math.Abs(offsetMs);

          if (absMs <= bestAbsMs)
          {
            bestAbsMs = absMs;
            bestDrag = data;
            bestWindowId = windowId;
          }
        }
      }

      if (bestDrag != null)
      {
        float offsetMs = BeatToMs(bestDrag.Start.AbsoluteValue - currentBeat);
        return (bestWindowId, bestDrag, offsetMs);
      }

      return null;
    }

    /// <summary>
    /// Finds the closest Tap/Hold note across ALL windows (skipping blocked windows).
    /// </summary>
    public (string WindowId, NoteData Note)? FindClosestNote(NoteType type, float currentBeat, float maxWindowMs, Dictionary<string, bool> inputBlocked)
    {
      string bestWindowId = null;
      NoteData closest = null;
      float bestAbsMs = maxWindowMs;

      foreach (var pair in _windowStates)
      {
        string windowId = pair.Key;

        if (inputBlocked != null && inputBlocked.TryGetValue(windowId, out bool blocked) && blocked)
          continue;

        foreach (var data in pair.Value.ActiveNotes)
        {
          if (data.IsEvaluated) continue;

          bool matches = data.Type == type
            || (type == NoteType.Tap && data.Type == NoteType.Hold);

          if (!matches) continue;
          if (data.Type == NoteType.Drag) continue;

          float offsetMs = BeatToMs(data.Start.AbsoluteValue - currentBeat);
          float absMs = Math.Abs(offsetMs);

          if (absMs < bestAbsMs)
          {
            bestAbsMs = absMs;
            closest = data;
            bestWindowId = windowId;
          }
        }
      }

      if (closest != null)
        return (bestWindowId, closest);

      return null;
    }

    /// <summary>
    /// Removes a specific note from its window's active list and frees its visual.
    /// </summary>
    public void ConsumeNote(string windowId, NoteData note)
    {
      if (_noteVisuals.TryGetValue(note, out var visual))
      {
        ReturnToPool(visual);
        _noteVisuals.Remove(note);
      }

      if (_windowStates.TryGetValue(windowId, out var state))
      {
        state.ActiveNotes.Remove(note);
      }
    }

    private float BeatToMs(float beats)
    {
      if (_timeManager?.Metronome == null) return 0f;
      float bps = _timeManager.Metronome.GetCurrentBPS(_timeManager.CurrentTime);
      return bps > 0 ? (beats / bps) * 1000f : 0f;
    }
  }
}
