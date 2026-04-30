using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Behaviors;
using Winithm.Core.Data;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{

  /// <summary>
  /// Manages note spawning, rendering, and lifecycle for all windows.
  /// Single instance with shared object pool.
  /// </summary>
  public class NoteController : Node
  {

    public event Action<string, NoteData> OnActiveHoldTick;
    public event Action<string, NoteData> OnActiveHoldEnded;
    public event Action<string, NoteData, double> OnDragReady;
    public event Action<string, NoteData> OnNoteMiss;
    public event Action<string, NoteData> OnAutoHit;

    [Export] public float PlayerNoteSize = 1f;
    [Export] public float PlayerNoteSpeed = 1f;

    public bool Autoplay { get; private set; } = false;

    public static readonly float NOTE_SPEED_PIXELS_PER_SEC = 72f;

    private static readonly Color NOTE_COLOR_DEFAULT = new Color(1f, 1f, 1f, 1f);
    private static readonly Color NOTE_COLOR_EVALUATED = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    /// <summary>Off-screen margin multiplier relative to note head height.</summary>
    private const float OFF_SCREEN_MARGIN_FACTOR = 3f;

    private PackedScene _noteScene;
    private Metronome _metronome;
    private NodePool<Note> _notePool;
    private double _lastBeat = double.MinValue;

    private Dictionary<string, WindowNoteState> _windowStates = new Dictionary<string, WindowNoteState>();
    private List<(string WindowId, NoteData Note)> _activeHoldsCache = new List<(string, NoteData)>();

    private class WindowNoteState
    {
      public WindowData WindowData;
      public Window WindowVisual;

      public Dictionary<NoteData, Note> NoteVisualMap = new Dictionary<NoteData, Note>();
      public Dictionary<double, Note> ChordHighlightMap = new Dictionary<double, Note>();

      public Dictionary<NoteSide, int> RenderCursors = new Dictionary<NoteSide, int>();
      public Dictionary<NoteSide, int> EvalCursors = new Dictionary<NoteSide, int>();

      public HashSet<NoteData> ActiveHolds = new HashSet<NoteData>();
      public List<NoteData> PendingRemovals = new List<NoteData>();

      public ulong AutoFireSessionToken = 1;
      public ulong FrameSessionToken = 1;
      public double LastBeat = double.MinValue;
    }

    // =============================================
    // Initialization
    // =============================================

    public void Initialize(Metronome metronome, bool autoplay = false)
    {
      Autoplay = autoplay;
      _metronome = metronome;
      _noteScene = GD.Load<PackedScene>("res://Winithm.Core/Resources/Sprites/Note.tscn");
      _notePool = new NodePool<Note>(this, _noteScene);
    }

    // =============================================
    // Window Registration
    // =============================================

    public void RegisterWindow(string windowId, WindowData windowData, Window windowVisual)
    {
      if (_windowStates.ContainsKey(windowId)) return;

      var state = new WindowNoteState() { WindowData = windowData, WindowVisual = windowVisual };

      foreach (NoteSide side in Enum.GetValues(typeof(NoteSide)))
      {
        state.RenderCursors[side] = 0;
        state.EvalCursors[side] = 0;
      }

      _windowStates[windowId] = state;
    }

    public void UnregisterWindow(string windowId)
    {
      if (!_windowStates.TryGetValue(windowId, out var state)) return;

      state.AutoFireSessionToken++;
      foreach (var noteVisual in state.NoteVisualMap.Values)
        ReturnToPool(noteVisual);

      _windowStates.Remove(windowId);
    }

    public override void _ExitTree()
    {
      base._ExitTree();

      foreach (var state in _windowStates.Values)
      {
        foreach (var noteVisual in state.NoteVisualMap.Values)
        {
          if (IsInstanceValid(noteVisual)) noteVisual.QueueFree();
        }
        state.NoteVisualMap.Clear();
      }

      _notePool?.Dispose();
    }

    // =============================================
    // Per-Frame Update
    // =============================================

    public void Update(double currentBeat)
    {
      if (currentBeat == _lastBeat) return;
      ForceUpdate(currentBeat, false);
    }

    public void ForceUpdate(double currentBeat, bool _force = true)
    {
      foreach (var entry in _windowStates)
        ProcessWindow(entry.Key, entry.Value, currentBeat, _force);

      _lastBeat = currentBeat;
    }

    private void ProcessWindow(
      string windowId,
      WindowNoteState state,
      double currentBeat,
      bool force
    )
    {
      if (state.WindowVisual == null) return;
      if (currentBeat == state.LastBeat && !force) return;

      bool isBackward = currentBeat < state.LastBeat;

      ProcessActiveHoldNotes(windowId, state, currentBeat);

      Vector2 playerAreaSize = state.WindowVisual.PlayerAreaSize;
      Vector2 windowSize = state.WindowVisual.WindowSize;

      double beatsPerSecond = _metronome.GetCurrentBPS(currentBeat);
      float pixelsPerBeat =
        NOTE_SPEED_PIXELS_PER_SEC * PlayerNoteSpeed / (float)(
          beatsPerSecond > 0f ? beatsPerSecond : 2f
        );

      float noteHeadHeight = PlayerNoteSize * Mathf.Min(
        playerAreaSize.x, 
        playerAreaSize.y
      ) * Note.NOTE_HEAD_HEIGHT_RATIO;

      float offScreenMarginPx = noteHeadHeight * OFF_SCREEN_MARGIN_FACTOR;

      float viewportScale = Math.Min(
        playerAreaSize.x / Constants.Visual.DESIGN_RESOLUTION.x,
        playerAreaSize.y / Constants.Visual.DESIGN_RESOLUTION.y
      );

      state.ChordHighlightMap.Clear();

      foreach (var sideEntry in state.WindowData.Notes)
      {
        NoteSide side = sideEntry.Key;
        var noteList = sideEntry.Value;

        float viewportLengthPx = IsVerticalSide(side) ? windowSize.y : windowSize.x;
        int renderCursor = state.RenderCursors[side];

        // Pull cursor backward using precomputed MaxEndBeats (binary search)
        renderCursor = SyncCursorBackward(
          state, side, renderCursor, currentBeat, pixelsPerBeat, viewportScale, offScreenMarginPx
        );

        // Advance cursor past notes whose tail left the screen
        renderCursor = SyncCursorForward(
          state, noteList, renderCursor, currentBeat, pixelsPerBeat, viewportScale, offScreenMarginPx
        );

        state.RenderCursors[side] = renderCursor;

        EvaluateNoteLifecycle(windowId, state, side, currentBeat, isBackward);

        // Render visible notes
        for (int i = renderCursor; i < noteList.Count; i++)
        {
          NoteData note = noteList[i];

          if (ShouldSkipRendering(note, currentBeat)) continue;

          double noteStartBeat = note.StartBeat.AbsoluteValue;
          double noteEndBeat = noteStartBeat + note.Length;

          float headOffsetPx = state.WindowData.SpeedSteps.GetVisualOffset(
            currentBeat, noteStartBeat
          ) * pixelsPerBeat * viewportScale;

          // Notes beyond viewport: all subsequent are even further (sorted by StartBeat)
          if (headOffsetPx > viewportLengthPx + offScreenMarginPx) break;

          float tailOffsetPx = (note.Length == 0)
            ? headOffsetPx
            : state.WindowData.SpeedSteps.GetVisualOffset(currentBeat, noteEndBeat) * pixelsPerBeat * viewportScale;

          if (!state.NoteVisualMap.TryGetValue(note, out Note noteVisual))
            noteVisual = SpawnNote(state, note);

          noteVisual.Modulate = note.IsEvaluated ? NOTE_COLOR_EVALUATED : NOTE_COLOR_DEFAULT;

          PositionNoteVisual(
            side, note, noteVisual, headOffsetPx, tailOffsetPx, state
          );

          note.LastSeenFrameSessionToken = state.FrameSessionToken;
        }
      }

      // Return off-screen visuals to pool
      CollectStaleNoteVisuals(state);

      state.FrameSessionToken++;
      state.LastBeat = currentBeat;
    }

    private bool IsVerticalSide(NoteSide side)
    {
      return side == NoteSide.Top || side == NoteSide.Bottom;
    }

    /// <summary>
    /// Skip rendering for notes that have already been consumed or are behind playback,
    /// except for active hold notes whose tail hasn't passed yet.
    /// </summary>
    private bool ShouldSkipRendering(NoteData note, double currentBeat)
    {
      bool isConsumed = Autoplay || note.IsEvaluated || !note.IsHittable;
      if (!isConsumed) return false;

      double noteStartBeat = note.StartBeat.AbsoluteValue;
      double noteEndBeat = noteStartBeat + note.Length;

      // Active hold: keep rendering until tail passes
      if (note.Type == NoteType.Hold && currentBeat <= noteEndBeat) return false;

      // Note head is behind playback
      return currentBeat > noteStartBeat;
    }

    // =============================================
    // Cursor Synchronization
    // =============================================

    private int SyncCursorBackward(
      WindowNoteState state,
      NoteSide side,
      int cursor,
      double currentBeat,
      float pixelsPerBeat,
      float viewportScale,
      float offScreenMarginPx
    )
    {
      state.WindowData.Notes.MaxEndBeats.TryGetValue(side, out double[] maxEndBeats);
      if (cursor <= 0 || maxEndBeats == null) return cursor;

      int lo = 0, hi = cursor - 1, result = cursor;
      while (lo <= hi)
      {
        int mid = (lo + hi) / 2;
        float distancePx = state.WindowData.SpeedSteps.GetVisualOffset(
          currentBeat, maxEndBeats[mid]
        ) * pixelsPerBeat * viewportScale;

        if (distancePx >= -offScreenMarginPx)
        {
          result = mid;
          hi = mid - 1;
        }
        else
        {
          lo = mid + 1;
        }
      }

      return result;
    }

    private int SyncCursorForward(
      WindowNoteState state,
      List<NoteData> noteList,
      int cursor,
      double currentBeat,
      float pixelsPerBeat,
      float viewportScale,
      float offScreenMarginPx
    )
    {
      while (cursor < noteList.Count)
      {
        double noteEndBeat = noteList[cursor].StartBeat.AbsoluteValue + noteList[cursor].Length;
        float distancePx =
          state.WindowData.SpeedSteps.GetVisualOffset(currentBeat, noteEndBeat) * pixelsPerBeat * viewportScale;

        if (distancePx < -offScreenMarginPx) cursor++;
        else break;
      }

      return cursor;
    }

    // =============================================
    // Spawn & Pool
    // =============================================

    private Note SpawnNote(WindowNoteState state, NoteData note)
    {
      Note noteVisual = _notePool.Get();

      Node parentLayer = (note.Type == NoteType.Focus)
        ? state.WindowVisual.FocusNoteLayer
        : state.WindowVisual.NoteLayer;

      if (noteVisual.GetParent() != parentLayer)
      {
        if (noteVisual.GetParent() != null) noteVisual.GetParent().RemoveChild(noteVisual);
        parentLayer.AddChild(noteVisual);
      }

      // Newer notes render on top
      parentLayer.MoveChild(noteVisual, -1);

      state.NoteVisualMap[note] = noteVisual;
      return noteVisual;
    }

    /// <summary>Removes a note's visual and returns it to the pool.</summary>
    public void ConsumeNote(string windowId, NoteData note)
    {
      if (_windowStates.TryGetValue(windowId, out var state))
      {
        if (state.NoteVisualMap.TryGetValue(note, out var noteVisual))
        {
          ReturnToPool(noteVisual);
          state.NoteVisualMap.Remove(note);
        }
        state.ActiveHolds.Remove(note);
      }
    }

    private void ReturnToPool(Note noteVisual)
    {
      noteVisual.Visible = false;
      if (noteVisual.GetParent() != null) noteVisual.GetParent().RemoveChild(noteVisual);
      _notePool.Release(noteVisual);
    }

    private void CollectStaleNoteVisuals(WindowNoteState state)
    {
      state.PendingRemovals.Clear();
      foreach (var note in state.NoteVisualMap.Keys)
      {
        if (note.LastSeenFrameSessionToken != state.FrameSessionToken)
          state.PendingRemovals.Add(note);
      }

      foreach (var note in state.PendingRemovals)
      {
        ReturnToPool(state.NoteVisualMap[note]);
        state.NoteVisualMap.Remove(note);
      }
    }

    // =============================================
    // Note Positioning
    // =============================================

    private void PositionNoteVisual(
      NoteSide side,
      NoteData note,
      Note noteVisual,
      float headOffsetPx,
      float tailOffsetPx,
      WindowNoteState state)
    {
      Vector2 playerAreaSize = state.WindowVisual.PlayerAreaSize;
      Vector2 windowSize = state.WindowVisual.WindowSize;

      float headHeight = 
        noteVisual.NoteSize * Mathf.Min(
          playerAreaSize.x, playerAreaSize.y
        ) * Note.NOTE_HEAD_HEIGHT_RATIO;
      float bodyHeight = 0f;

      if (note.Type == NoteType.Hold)
      {
        bodyHeight = Mathf.Max(0f, tailOffsetPx - headOffsetPx - headHeight);
        if (headOffsetPx < 0f)
        {
          headOffsetPx = 0f;
          bodyHeight = Mathf.Max(0f, tailOffsetPx - headHeight);
        }
      }

      // Width depends on whether the note sits on a vertical or horizontal edge
      float noteWidth = IsVerticalSide(side)
        ? windowSize.x * note.Width
        : windowSize.y * note.Width;

      noteVisual.Width = noteWidth;
      noteVisual.NoteSize = PlayerNoteSize;
      noteVisual.PlayerAreaSize = playerAreaSize;
      noteVisual.BodyHeight = bodyHeight;

      ResourcePack resourcePack = note.ResourcePack.HasValue
        ? note.ResourcePack.Value
        : ResourcePackManager.Instance.GetActiveResourcePack();
      noteVisual.SetNoteType(note.Type, resourcePack);

      // Highlight notes sharing the same start beat (chords)
      ApplyChordHighlight(state, note, noteVisual);

      // Lateral position: center of note width, offset by X within remaining space
      float lateralPosition = note.Width / 2f + note.X * (1f - note.Width);

      switch (side)
      {
        case NoteSide.Bottom:
          noteVisual.Position = new Vector2(
            windowSize.x * lateralPosition,
            windowSize.y - headOffsetPx
          );
          noteVisual.RotationDegrees = 0f;
          break;
        case NoteSide.Top:
          noteVisual.Position = new Vector2(
            windowSize.x * lateralPosition,
            headOffsetPx
          );
          noteVisual.RotationDegrees = 180f;
          break;
        case NoteSide.Right:
          noteVisual.Position = new Vector2(
            windowSize.x - headOffsetPx,
            windowSize.y * lateralPosition
          );
          noteVisual.RotationDegrees = -90f;
          break;
        case NoteSide.Left:
          noteVisual.Position = new Vector2(
            headOffsetPx,
            windowSize.y * lateralPosition
          );
          noteVisual.RotationDegrees = 90f;
          break;
      }

      noteVisual.UpdateVisual();
    }

    private void ApplyChordHighlight(WindowNoteState state, NoteData note, Note noteVisual)
    {
      noteVisual.SetNoteHighlighting(false);
      double startBeat = note.StartBeat.AbsoluteValue;

      if (state.ChordHighlightMap.TryGetValue(startBeat, out var existingVisual))
      {
        noteVisual.SetNoteHighlighting(true);
        existingVisual.SetNoteHighlighting(true);
      }
      else
      {
        state.ChordHighlightMap[startBeat] = noteVisual;
      }
    }

    // =============================================
    // Note Lifecycle (Miss + Auto-fire)
    // =============================================

    /// <summary>
    /// Per-frame: auto-hits ghost/autoplay notes, fires OnDragReady for Drag notes
    /// in judgement zone, and fires OnNoteMiss for notes past the timing window.
    /// </summary>
    private void EvaluateNoteLifecycle(
      string windowId,
      WindowNoteState state,
      NoteSide side,
      double currentBeat,
      bool isBackward)
    {
      if (isBackward)
      {
        state.AutoFireSessionToken++;
        state.EvalCursors[side] = Math.Min(state.EvalCursors[side], state.RenderCursors[side]);
        return;
      }

      double dragWindowMs = Constants.HitResult.TimmingWindowMs[HitResultType.Bad];
      double missWindowMs = Constants.HitResult.TimmingWindowMs[HitResultType.Miss];
      int evalCursor = state.EvalCursors[side];
      var noteList = state.WindowData.Notes[side];

      while (evalCursor < noteList.Count)
      {
        NoteData note = noteList[evalCursor];

        if (note.IsEvaluated) { evalCursor++; continue; }
        if (note.IsHoldActive) { evalCursor++; continue; }
        if (note.StartBeat.AbsoluteValue > currentBeat) break;

        double elapsedMs = _metronome.ToDeltaMilliSeconds(
          note.StartBeat.AbsoluteValue, currentBeat
        );

        bool shouldAutoHit =
          ((Autoplay && !note.IsMutedGhost) || note.IsLoudGhost)
          && elapsedMs >= 0f
          && note.AutoFiredSessionToken != state.AutoFireSessionToken;

        if (shouldAutoHit)
        {
          note.AutoFiredSessionToken = state.AutoFireSessionToken;
          if (note.Type == NoteType.Hold)
          {
            note.IsHoldActive = true;
            state.ActiveHolds.Add(note);
          }
          OnAutoHit?.Invoke(windowId, note);
          evalCursor++;
          continue;
        }

        // Muted ghost: skip without evaluation
        if (note.IsMutedGhost && elapsedMs >= 0f)
        {
          evalCursor++;
          continue;
        }

        // Drag notes: notify when inside judgement zone
        if (note.Type == NoteType.Drag && note.IsHittable && elapsedMs <= dragWindowMs)
        {
          OnDragReady?.Invoke(windowId, note, elapsedMs);
        }

        // Miss: exceeded timing window
        if (elapsedMs > missWindowMs)
        {
          if (note.IsHittable) OnNoteMiss?.Invoke(windowId, note);
          evalCursor++;
        }
        else
        {
          break; // Within timing window, waiting for player input
        }
      }

      state.EvalCursors[side] = evalCursor;
    }

    private void ProcessActiveHoldNotes(string windowId, WindowNoteState state, double currentBeat)
    {
      state.PendingRemovals.Clear();

      foreach (var holdNote in state.ActiveHolds)
      {
        double holdStartBeat = holdNote.StartBeat.AbsoluteValue;
        double holdEndBeat = holdStartBeat + holdNote.Length;

        // Defensive reset: playback rewound before hold start and player hasn't engaged yet
        if (currentBeat < holdStartBeat && double.IsNaN(holdNote.HoldStartOffsetMs))
        {
          holdNote.IsHoldActive = false;
          state.PendingRemovals.Add(holdNote);
          continue;
        }

        // Hold tail reached: finalize scoring
        if (currentBeat >= holdEndBeat)
        {
          if (holdNote.IsHittable && !holdNote.IsEvaluated)
          {
            OnActiveHoldEnded?.Invoke(windowId, holdNote);
          }

          holdNote.IsHoldActive = false;
          state.PendingRemovals.Add(holdNote);
          continue;
        }

        // Already judged: stop tracking
        if (holdNote.IsEvaluated)
        {
          holdNote.IsHoldActive = false;
          state.PendingRemovals.Add(holdNote);
          continue;
        }

        // Hold still active: emit sustain tick
        OnActiveHoldTick?.Invoke(windowId, holdNote);
      }

      foreach (var holdNote in state.PendingRemovals)
      {
        state.ActiveHolds.Remove(holdNote);
      }
    }


    // TODO: Move two Methods to HitController
    // =============================================
    // Hit Evaluation API
    // =============================================

    /// <summary>
    /// Finds all hittable Focus/Close notes. If closest is Perfect, absorbs all
    /// Perfect notes (max 1 per window). If sloppy, absorbs only the closest.
    /// </summary>
    public List<(string WindowId, NoteData Note, float OffsetMs)> TryEvaluateAll(NoteType type, float currentBeat)
    {
      double missWindowMs = Constants.HitResult.TimmingWindowMs[HitResultType.Miss];
      double perfectWindowMs = Constants.HitResult.TimmingWindowMs[HitResultType.Perfect];

      var candidates = new List<(string WindowId, NoteData Note, float OffsetMs)>();
      float closestAbsMs = float.MaxValue;
      (string WindowId, NoteData Note, float OffsetMs)? closestCandidate = null;

      // Pass 1: Gather all hittable notes and track closest
      foreach (var entry in _windowStates)
      {
        string windowId = entry.Key;
        var state = entry.Value;

        if (type != NoteType.Focus && state.WindowVisual.UnFocus) continue;

        foreach (var sideEntry in state.WindowData.Notes)
        {
          int cursor = state.EvalCursors[sideEntry.Key];
          var noteList = sideEntry.Value;

          for (int i = cursor; i < noteList.Count; i++)
          {
            NoteData note = noteList[i];
            if (note.IsEvaluated || note.Type != type || !note.IsHittable) continue;

            double offsetMs = _metronome.ToDeltaMilliSeconds(
              note.StartBeat.AbsoluteValue, currentBeat
            );

            // Sorted by time: all subsequent notes are even further ahead
            if (offsetMs > missWindowMs) break;

            float absMs = Mathf.Abs((float)offsetMs);
            if (absMs <= missWindowMs)
            {
              var candidate = (windowId, note, (float)offsetMs);
              candidates.Add(candidate);

              if (absMs < closestAbsMs)
              {
                closestAbsMs = absMs;
                closestCandidate = candidate;
              }
            }
          }
        }
      }

      // Pass 2: Smart grouping based on closest hit quality
      var results = new List<(string, NoteData, float)>();

      if (closestCandidate.HasValue)
      {
        if (closestAbsMs <= perfectWindowMs)
        {
          // Perfect hit: group all Perfect notes (max 1 per window)
          var bestPerWindow = new Dictionary<string, (string WindowId, NoteData Note, float OffsetMs)>();
          foreach (var candidate in candidates)
          {
            float candidateAbsMs = Math.Abs(candidate.OffsetMs);
            if (candidateAbsMs <= perfectWindowMs)
            {
              if (!bestPerWindow.ContainsKey(candidate.WindowId) ||
                  candidateAbsMs < Math.Abs(bestPerWindow[candidate.WindowId].OffsetMs))
              {
                bestPerWindow[candidate.WindowId] = candidate;
              }
            }
          }
          results.AddRange(bestPerWindow.Values);
        }
        else
        {
          // Sloppy hit: only consume closest to prevent chain-downgrading
          results.Add(closestCandidate.Value);
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
      NoteData closestNote = null;
      double closestAbsMs = Constants.HitResult.TimmingWindowMs[HitResultType.Miss];

      foreach (var entry in _windowStates)
      {
        string windowId = entry.Key;
        WindowNoteState state = entry.Value;

        if (state.WindowVisual.UnFocus) continue;

        foreach (var sideEntry in state.WindowData.Notes)
        {
          int cursor = state.EvalCursors[sideEntry.Key];
          var noteList = sideEntry.Value;

          for (int i = cursor; i < noteList.Count; i++)
          {
            NoteData note = noteList[i];
            if (note.IsEvaluated || note.IsHoldActive || !note.IsHittable) continue;

            bool typeMatches = note.Type == type || (type == NoteType.Tap && note.Type == NoteType.Hold);
            if (!typeMatches) continue;

            double offsetMs = _metronome.ToDeltaMilliSeconds(
              note.StartBeat.AbsoluteValue, currentBeat
            );

            // Sorted by time: all subsequent notes are even further ahead
            if (offsetMs > closestAbsMs) break;

            float absMs = Mathf.Abs((float)offsetMs);
            if (absMs < closestAbsMs)
            {
              closestAbsMs = absMs;
              closestNote = note;
              bestWindowId = windowId;
            }
          }
        }
      }

      return closestNote != null ? (bestWindowId, closestNote) : ((string, NoteData)?)null;
    }

    /// <summary>Marks a note as an active hold and tracks it for completion.</summary>
    public void SetHoldActive(string windowId, NoteData note)
    {
      if (_windowStates.TryGetValue(windowId, out var state))
      {
        note.IsHoldActive = true;
        state.ActiveHolds.Add(note);
      }
    }

    /// <summary>Returns all currently active hold notes across all windows.</summary>
    public List<(string WindowId, NoteData Note)> GetActiveHolds()
    {
      _activeHoldsCache.Clear();
      foreach (var entry in _windowStates)
      {
        foreach (var holdNote in entry.Value.ActiveHolds)
        {
          if (!holdNote.IsEvaluated)
            _activeHoldsCache.Add((entry.Key, holdNote));
        }
      }
      return _activeHoldsCache;
    }

  }
}
