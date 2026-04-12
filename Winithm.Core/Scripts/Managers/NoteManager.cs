using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Behaviors;
using Winithm.Core.Data;
using Winithm.Core.Logic;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages note spawning, rendering, and lifecycle for all windows.
  /// Single instance with shared object pool.
  /// </summary>
  public class NoteManager : Node
  {
    public event Action<string, NoteData> OnNoteHit;
    public event Action<string, NoteData> OnNoteMiss;
    public event Action<string, NoteData, float> OnDragReady;
    public event Action<string, NoteData> OnActiveHoldEnded;
    public event Action<string, NoteData> OnActiveHoldTick;
    public event Action<string, NoteData> OnAutoHit;

    [Export] public float PlayerNoteSize = 1f;
    [Export] public float PlayerNoteSpeed = 1f;

    public bool Autoplay { get; private set; } = false;

    private float _lastBeat = float.MinValue;
    private TimeManager _timeManager;
    private PackedScene _noteScene;

    private class WindowNoteState
    {
      public WindowData Data;
      public Window Visual;
      public Dictionary<NoteData, Note> NoteVisuals = new Dictionary<NoteData, Note>();
      public Dictionary<NoteSide, int> SideCursors = new Dictionary<NoteSide, int>();
      public Dictionary<NoteSide, int> EvalCursors = new Dictionary<NoteSide, int>();
      public HashSet<NoteData> ActiveNotesThisFrame = new HashSet<NoteData>();
      public HashSet<NoteData> ActiveHolds = new HashSet<NoteData>();
      public List<NoteData> KeysToRemove = new List<NoteData>();
      public SpeedCalculator.FrameCache SpeedCache = new SpeedCalculator.FrameCache();
      public float LastBeat = float.MinValue;
    }

    private Dictionary<string, WindowNoteState> _windowStates = new Dictionary<string, WindowNoteState>();
    private NodePool<Note> _notePool;

    public static readonly float NOTE_SPEED_IN_PIXEL_PER_SEC = 720f;

    public void Initialize(TimeManager timeManager, bool autoplay = false)
    {
      Autoplay = autoplay;
      _timeManager = timeManager;
      _noteScene = GD.Load<PackedScene>("res://Winithm.Core/Resources/Sprites/Note.tscn");
      _notePool = new NodePool<Note>(this, _noteScene);
    }

    // =============================================
    // Window Registration
    // =============================================

    public void RegisterWindow(string windowId, WindowData wd, Window window)
    {
      if (_windowStates.ContainsKey(windowId)) return;

      var state = new WindowNoteState() { Data = wd, Visual = window };

      foreach (NoteSide side in Enum.GetValues(typeof(NoteSide)))
      {
        state.SideCursors[side] = 0;
        state.EvalCursors[side] = 0;
      }

      _windowStates[windowId] = state;
    }

    public void UnregisterWindow(string windowId)
    {
      if (!_windowStates.TryGetValue(windowId, out var state)) return;
      foreach (var visual in state.NoteVisuals.Values)
        ReturnToPool(visual);
      _windowStates.Remove(windowId);
    }

    public override void _ExitTree()
    {
      base._ExitTree();

      foreach (var state in _windowStates.Values)
      {
        foreach (var visual in state.NoteVisuals.Values)
        {
          if (IsInstanceValid(visual)) visual.QueueFree();
        }
        state.NoteVisuals.Clear();
      }

      _notePool?.Dispose();
    }

    // =============================================
    // Per-Frame Update
    // =============================================

    public void Update(float currentBeat, bool isScrubbing = false)
    {
      if (currentBeat == _lastBeat) return;

      ForceUpdate(currentBeat, isScrubbing);
    }

    public void ForceUpdate(float currentBeat, bool isScrubbing = true)
    {
      foreach (var pair in _windowStates)
        ProcessWindow(pair.Key, pair.Value, currentBeat, isScrubbing);

      _lastBeat = currentBeat;
    }

    private void ProcessWindow(string windowId, WindowNoteState state, float currentBeat, bool isScrubbing)
    {
      if (state.Visual == null) return;
      if (currentBeat == state.LastBeat) return;

      bool isRewind = currentBeat < state.LastBeat;

      ProcessActiveHoldNotes(windowId, state, currentBeat);


      Vector2 playerAreaSize = state.Visual.PlayerAreaSize;
      float currentBps = _timeManager.Metronome.GetCurrentBPS(_timeManager.CurrentTime);
      float basePixelsPerBeat = NOTE_SPEED_IN_PIXEL_PER_SEC * PlayerNoteSpeed / (currentBps > 0f ? currentBps : 2f);

      float noteH = PlayerNoteSize * Mathf.Min(playerAreaSize.x, playerAreaSize.y) * Note.NOTE_HEAD_HEIGHT_RATIO;
      float limitPx = noteH * 3f;

      state.ActiveNotesThisFrame.Clear();

      foreach (var sideNotes in state.Data.Notes)
      {
        NoteSide side = sideNotes.Key;
        var notes = sideNotes.Value;

        float scale = Math.Min(
          playerAreaSize.x / Constants.Visual.DESIGN_RESOLUTION.x,
          playerAreaSize.y / Constants.Visual.DESIGN_RESOLUTION.y
        );
        float limitDistPx =
          (side == NoteSide.Top || side == NoteSide.Bottom)
          ? state.Visual.WindowSize.y
          : state.Visual.WindowSize.x;

        int cursor = state.SideCursors[side];

        // BACKWARD SYNC: Pull cursor back using precomputed MaxEndBeats
        state.Data.MaxEndBeats.TryGetValue(side, out float[] maxEndBeats);
        while (cursor > 0 && maxEndBeats != null)
        {
          float maxEnd = maxEndBeats[cursor - 1];
          float distPx = SpeedCalculator.GetVisualOffset(state.SpeedCache, state.Data.SpeedSteps, currentBeat, maxEnd) * basePixelsPerBeat * scale;
          if (distPx >= -limitPx) cursor--;
          else break;
        }

        // FORWARD SYNC: Advance past notes whose EndBeat left the screen
        while (cursor < notes.Count)
        {
          float endBeat = notes[cursor].StartBeat.AbsoluteValue + notes[cursor].Length;
          float distPx = SpeedCalculator.GetVisualOffset(state.SpeedCache, state.Data.SpeedSteps, currentBeat, endBeat) * basePixelsPerBeat * scale;
          if (distPx < -limitPx) cursor++;
          else break;
        }

        state.SideCursors[side] = cursor;

        // LIFECYCLE: Drag auto-fire + Miss evaluation
        EvaluateNoteLifecycle(windowId, state, side, currentBeat, isRewind);

        // RENDER
        for (int i = cursor; i < notes.Count; i++)
        {
          NoteData data = notes[i];

          float headDistPx = SpeedCalculator.GetVisualOffset(state.SpeedCache, state.Data.SpeedSteps, currentBeat, data.StartBeat.AbsoluteValue) * basePixelsPerBeat * scale;

          // Early exit: notes beyond viewport (sorted by StartBeat)
          if (headDistPx > limitDistPx + limitPx) break;

          float endBeat = data.StartBeat.AbsoluteValue + data.Length;

          // Missed/dropped Hold notes stay visible but dimmed until EndBeat passes
          bool isMissedHold =
            data.IsEvaluated &&
            !isRewind &&
            !isScrubbing &&
            data.Type == NoteType.Hold &&
            currentBeat < endBeat;

          // Hide evaluated notes during normal play; show during rewind/scrubbing
          if (data.IsEvaluated && !isRewind && !isScrubbing && !isMissedHold) continue;

          float endDistPx =
            endBeat == data.StartBeat.AbsoluteValue
            ? headDistPx
            : SpeedCalculator.GetVisualOffset(state.SpeedCache, state.Data.SpeedSteps, currentBeat, endBeat) * basePixelsPerBeat * scale;

          if (!state.NoteVisuals.TryGetValue(data, out Note visual))
            visual = SpawnNote(windowId, state, data);

          // Dim missed holds, reset pool reuse artifacts otherwise
          visual.Modulate = isMissedHold
            ? new Color(0.5f, 0.5f, 0.5f, 0.5f)
            : new Color(1f, 1f, 1f, 1f);

          UpdateNoteVisual(data, visual, headDistPx, endDistPx, state.Visual);
          state.ActiveNotesThisFrame.Add(data);
        }
      }

      // CLEANUP: Return off-screen visuals to pool
      state.KeysToRemove.Clear();
      foreach (var key in state.NoteVisuals.Keys)
      {
        if (!state.ActiveNotesThisFrame.Contains(key))
          state.KeysToRemove.Add(key);
      }

      foreach (var key in state.KeysToRemove)
      {
        ReturnToPool(state.NoteVisuals[key]);
        state.NoteVisuals.Remove(key);
        state.ActiveHolds.Remove(key);
      }

      state.LastBeat = currentBeat;
    }

    // =============================================
    // Spawn & Pool
    // =============================================

    private Note SpawnNote(string windowId, WindowNoteState state, NoteData data)
    {
      Note visual = _notePool.Get();

      Control parent = (data.Type == NoteType.Focus)
        ? state.Visual.FocusNoteLayer
        : state.Visual.NoteLayer;

      if (visual.GetParent() != parent)
      {
        if (visual.GetParent() != null) visual.GetParent().RemoveChild(visual);
        parent.AddChild(visual);
      }
      // Newer notes drawn on top of older ones
      parent.MoveChild(visual, -1);

      state.NoteVisuals[data] = visual;
      return visual;
    }

    /// <summary>Removes a note's visual and returns it to the pool.</summary>
    public void ConsumeNote(string windowId, NoteData note)
    {
      if (_windowStates.TryGetValue(windowId, out var state))
      {
        if (state.NoteVisuals.TryGetValue(note, out var visual))
        {
          ReturnToPool(visual);
          state.NoteVisuals.Remove(note);
        }
        state.ActiveHolds.Remove(note);
      }
    }

    private void ReturnToPool(Note visual)
    {
      visual.Visible = false;
      if (visual.GetParent() != null) visual.GetParent().RemoveChild(visual);
      _notePool.Release(visual);
    }

    // =============================================
    // Note Positioning
    // =============================================

    private void UpdateNoteVisual(NoteData data, Note visual, float headDistPx, float endDistPx, Window windowVisual)
    {
      Vector2 playerAreaSize = windowVisual.PlayerAreaSize;
      Vector2 winSize = windowVisual.WindowSize;

      float headH = visual.NoteSize * Mathf.Min(playerAreaSize.x, playerAreaSize.y) * Note.NOTE_HEAD_HEIGHT_RATIO;
      float bodyH = 0f;

      if (data.Type == NoteType.Hold)
      {
        bodyH = Mathf.Max(0f, endDistPx - headDistPx - headH);
        if (headDistPx < 0f)
        {
          headDistPx = 0f;
          bodyH = Mathf.Max(0f, endDistPx - headH);
        }
      }

      switch (data.Side)
      {
        case NoteSide.Bottom:
        case NoteSide.Top:
          visual.Width = winSize.x * data.Width;
          break;
        case NoteSide.Left:
        case NoteSide.Right:
          visual.Width = winSize.y * data.Width;
          break;
      }

      visual.NoteSize = PlayerNoteSize;
      visual.PlayerAreaSize = playerAreaSize;
      visual.SetNoteType(data.Type);
      visual.BodyHeight = bodyH;

      switch (data.Side)
      {
        case NoteSide.Bottom:
          visual.RectPosition = new Vector2(
            winSize.x * (data.Width / 2f + data.X * (1f - data.Width)),
            winSize.y - headDistPx
          );
          visual.RectRotation = 0f;
          break;
        case NoteSide.Top:
          visual.RectPosition = new Vector2(
            winSize.x * (data.Width / 2f + data.X * (1f - data.Width)),
            headDistPx
          );
          visual.RectRotation = 180f;
          break;
        case NoteSide.Right:
          visual.RectPosition = new Vector2(
            winSize.x - headDistPx,
            winSize.y * (data.Width / 2f + data.X * (1f - data.Width))
          );
          visual.RectRotation = -90f;
          break;
        case NoteSide.Left:
          visual.RectPosition = new Vector2(
            headDistPx,
            winSize.y * (data.Width / 2f + data.X * (1f - data.Width))
          );
          visual.RectRotation = 90f;
          break;
      }

      visual.UpdateVisual();
    }

    // =============================================
    // Note Lifecycle (Miss + Drag auto-fire)
    // =============================================

    /// <summary>
    /// Per-frame: fires OnDragReady for Drag notes in judgement zone (0-120ms),
    /// fires OnNoteMiss for notes past the timing window.
    /// </summary>
    private void EvaluateNoteLifecycle(
      string windowId,
      WindowNoteState state,
      NoteSide side,
      float currentBeat,
      bool isRewind)
    {
      if (isRewind)
      {
        state.EvalCursors[side] = Math.Min(state.EvalCursors[side], state.SideCursors[side]);
        return;
      }

      float badWindowMs = Constants.HitResult.TimmingWindowMs[HitResultType.Bad];
      float missWindowMs = Constants.HitResult.TimmingWindowMs[HitResultType.Miss];
      int evalCursor = state.EvalCursors[side];
      var notes = state.Data.Notes[side];

      while (evalCursor < notes.Count)
      {
        NoteData currData = notes[evalCursor];

        if (currData.IsEvaluated) { evalCursor++; continue; }
        if (currData.IsHoldActive) { evalCursor++; continue; }

        if (currData.StartBeat.AbsoluteValue > currentBeat) break;

        float passedMs =
          _timeManager.Metronome.ToMiliSeconds(currentBeat - currData.StartBeat.AbsoluteValue);

        // LOUD GHOST: Auto-hit exactly on perfect timing
        if (currData.IsLoudGhost && passedMs >= 0f)
        {
          if (currData.Type == NoteType.Hold)
          {
            currData.IsHoldActive = true;
            state.ActiveHolds.Add(currData); // Push to ActiveHolds for ticks
            OnAutoHit?.Invoke(windowId, currData); // Fire initial head hit
          }
          else
          {
            OnAutoHit?.Invoke(windowId, currData);
          }
          evalCursor++;
          continue;
        }

        // MUTED GHOST: Consume silently
        if (currData.IsMutedGhost && passedMs >= 0f)
        {
          evalCursor++;
          continue;
        }

        // Drag notes: fire event in 0-120ms zone
        if (currData.Type == NoteType.Drag && currData.IsHittable && passedMs <= badWindowMs)
        {
          if (!currData.IsDragFired)
          {
            currData.IsDragFired = true;
            OnDragReady?.Invoke(windowId, currData, passedMs);
          }
          break; // Drag is earliest un-evaluated note; wait for HitManager or Miss timeout
        }

        // Miss: note head exceeded the timing window
        if (passedMs > missWindowMs)
        {
          if (currData.IsHittable) OnNoteMiss?.Invoke(windowId, currData);
          evalCursor++;
        }
        else
        {
          break;
        }
      }

      state.EvalCursors[side] = evalCursor;
    }

    private void ProcessActiveHoldNotes(string windowId, WindowNoteState state, float currentBeat)
    {
      state.KeysToRemove.Clear();

      foreach (var data in state.ActiveHolds)
      {
        // 1. Defensive reset: if playback is before the hold head, clear the active state.
        if
        (
        currentBeat < data.StartBeat.AbsoluteValue
        && data.HoldStartOffsetMs != float.NaN
        )
        {
          data.IsHoldActive = false;
          state.KeysToRemove.Add(data);
          continue;
        }

        // 2. Hold tail reached: end the active phase and let the caller finalize scoring.
        if (
          !data.IsEvaluated &&
          currentBeat >= data.StartBeat.AbsoluteValue + data.Length
        )
        {
          if (data.IsHittable)
          {
            OnActiveHoldEnded?.Invoke(windowId, data);
          }
          data.IsHoldActive = false;
          state.KeysToRemove.Add(data);
          continue;
        }

        // 3. Evaluated note: if the note is already judged (e.g., hit or missed), stop tracking it.
        if (data.IsEvaluated)
        {
          data.IsHoldActive = false;
          state.KeysToRemove.Add(data);
          continue;
        }

        // 4. Hold is still active between the head and tail, emit sustain tick.
        OnActiveHoldTick?.Invoke(windowId, data);
      }

      foreach (var key in state.KeysToRemove)
      {
        state.ActiveHolds.Remove(key);
      }
    }

    // =============================================
    // HitManager API
    // =============================================

    /// <summary>
    /// Broadcast hit: finds all hittable Focus/Close notes. Smart grouping logic:
    /// - If closest is Perfect → absorb all Perfect notes (max 1 per window)
    /// - If closest is sloppy → absorb only the closest one
    /// </summary>
    public List<(string WindowId, NoteData Note, float OffsetMs)> TryEvaluateAll(NoteType type, float currentBeat)
    {
      float missMs = Constants.HitResult.TimmingWindowMs[HitResultType.Miss];
      float perfectMs = Constants.HitResult.TimmingWindowMs[HitResultType.Perfect];

      var allValid = new List<(string WindowId, NoteData Note, float OffsetMs)>();
      float bestAbsMs = float.MaxValue;
      (string WindowId, NoteData Note, float OffsetMs)? closestItem = null;

      // Pass 1: Gather all hittable notes and find closest by iterating the temporal streams
      foreach (var pair in _windowStates)
      {
        string windowId = pair.Key;
        WindowNoteState state = pair.Value;

        if (type != NoteType.Focus && state.Visual.UnFocus) continue;

        foreach (var sideNotes in state.Data.Notes)
        {
          int cursor = state.EvalCursors[sideNotes.Key];
          var notes = sideNotes.Value;

          for (int i = cursor; i < notes.Count; i++)
          {
            NoteData data = notes[i];
            if (data.IsEvaluated || data.Type != type || !data.IsHittable) continue;

            float offsetMs =
            _timeManager.Metronome.ToMiliSeconds(data.StartBeat.AbsoluteValue)
            -
            _timeManager.Metronome.ToMiliSeconds(currentBeat); ;

            // Optimization: Notes are sorted chronologically. 
            // If this note is too far in the future, all subsequent ones are even further.
            if (offsetMs > missMs) break;

            float absMs = Math.Abs(offsetMs);
            if (absMs <= missMs)
            {
              var item = (windowId, data, offsetMs);
              allValid.Add(item);
              if (absMs < bestAbsMs)
              {
                bestAbsMs = absMs;
                closestItem = item;
              }
            }
          }
        }
      }

      // Pass 2: Smart grouping
      var results = new List<(string, NoteData, float)>();

      if (closestItem.HasValue)
      {
        if (bestAbsMs <= perfectMs)
        {
          // Perfect hit: group all Perfect notes (max 1 per window)
          var bestPerWindow = new Dictionary<string, (string WindowId, NoteData Note, float OffsetMs)>();
          foreach (var item in allValid)
          {
            if (Math.Abs(item.OffsetMs) <= perfectMs)
            {
              if (!bestPerWindow.ContainsKey(item.WindowId) ||
                  Math.Abs(item.OffsetMs) < Math.Abs(bestPerWindow[item.WindowId].OffsetMs))
              {
                bestPerWindow[item.WindowId] = item;
              }
            }
          }
          results.AddRange(bestPerWindow.Values);
        }
        else
        {
          // Sloppy hit: only consume closest to prevent chain-downgrading
          results.Add(closestItem.Value);
        }
      }

      return results;
    }

    /// <summary>
    /// Single-target hit: finds the closest Tap/Hold note across focused windows.
    /// </summary>
    public (string WindowId, NoteData Note)? FindClosestNote(NoteType type, float currentBeat)
    {
      string bestWindowId = null;
      NoteData closest = null;
      float bestAbsMs = Constants.HitResult.TimmingWindowMs[HitResultType.Miss];

      foreach (var pair in _windowStates)
      {
        string windowId = pair.Key;
        WindowNoteState state = pair.Value;

        if (state.Visual.UnFocus) continue;

        foreach (var sideNotes in state.Data.Notes)
        {
          int cursor = state.EvalCursors[sideNotes.Key];
          var notes = sideNotes.Value;

          for (int i = cursor; i < notes.Count; i++)
          {
            NoteData data = notes[i];
            if (data.IsEvaluated || data.IsHoldActive || !data.IsHittable) continue;

            bool matches = data.Type == type || (type == NoteType.Tap && data.Type == NoteType.Hold);
            if (!matches) continue;

            float offsetMs =
              _timeManager.Metronome.ToMiliSeconds(data.StartBeat.AbsoluteValue)
              -
              _timeManager.Metronome.ToMiliSeconds(currentBeat);

            // Optimization: Break early if we've passed the closest possible future note
            if (offsetMs > bestAbsMs) break;

            float absMs = Math.Abs(offsetMs);
            if (absMs < bestAbsMs)
            {
              bestAbsMs = absMs;
              closest = data;
              bestWindowId = windowId;
            }
          }
        }
      }

      return closest != null ? (bestWindowId, closest) : ((string, NoteData)?)null;
    }

    /// <summary>Marks a note as an active hold (Phase 1) and tracks it for completion.</summary>
    public void SetHoldActive(string windowId, NoteData note)
    {
      if (_windowStates.TryGetValue(windowId, out var state))
      {
        note.IsHoldActive = true;
        state.ActiveHolds.Add(note);
      }
    }

    private List<(string WindowId, NoteData Note)> _activeHoldsCache = new List<(string, NoteData)>();

    /// <summary>Returns all Hold notes in active Phase 1 state (waiting for Phase 2).</summary>
    public List<(string WindowId, NoteData Note)> GetActiveHolds()
    {
      _activeHoldsCache.Clear();
      foreach (var pair in _windowStates)
      {
        foreach (var data in pair.Value.ActiveHolds)
        {
          if (!data.IsEvaluated)
            _activeHoldsCache.Add((pair.Key, data));
        }
      }
      return _activeHoldsCache;
    }

  }
}
