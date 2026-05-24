using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Controllers;
using Winithm.Core.Data;
using Winithm.Core.Managers;
using Constants = Winithm.Core.Constants;

namespace Winithm.Client.Controllers.Gameplay
{
  /// <summary>
  /// Central input evaluator for gameplay.
  /// Handles keyboard input and routes to NoteController evaluation methods.
  ///
  /// Input rules:
  /// - Tap/Hold: Any key (except LShift/RShift). N simultaneous notes = N presses needed.
  /// - Hold sustain: At least one key must be held. Early release beyond Good window = miss.
  /// - Drag: Any key held OR within Good window (125ms) after last release = auto hit.
  /// - Focus (Tab): One press resolves ALL Focus notes across ALL windows.
  ///     Also ends focusable state on any currently-focusable windows.
  ///     Miss = AddStartFocusable on the missed note's window.
  /// - Close (Backslash): Like Focus but miss = SetUnresponsive.
  /// </summary>
  public class HitController : Node
  {
    public event Action<string, HitResult> OnHit;
    public event Action<string, HitResult> OnMiss;
    public event Action<string, NoteData, HitResultType> OnHitFXRequested;

    private AudioController _audioController;
    private NoteController _noteController;
    private WindowController _windowController;

    private int _keysHeldCount = 0;
    private double _lastKeyReleaseBeat = double.MinValue;

    // Cache to avoid re-registering event handlers
    private bool _eventsSubscribed = false;

    // Object pool for polyphonic hit sounds
    private NodePool<AudioStreamPlayer> _audioPool;
    private readonly Dictionary<NoteData, long> _lastHoldTickIndex = new Dictionary<NoteData, long>();
    private List<(string WindowId, NoteData Note)> _activeHoldsCache = new List<(string, NoteData)>();

    public override void _Ready()
    {
      base._Ready();

      _audioPool = new NodePool<AudioStreamPlayer>(
        parent: this,
        createFunc: () =>
        {
          var player = new AudioStreamPlayer();
          player.Connect("finished", this, nameof(OnAudioPlayerFinished), new Godot.Collections.Array { player });
          AddChild(player);
          return player;
        },
        defaultCapacity: 16
      );
    }

    private void OnAudioPlayerFinished(AudioStreamPlayer player)
    {
      _audioPool.Release(player);
    }

    public void Initialize(
      AudioController audioController,
      NoteController noteController,
      WindowController windowController
    )
    {
      _audioController = audioController;
      _noteController = noteController;
      _windowController = windowController;

      SubscribeNoteEvents();
    }

    private void SubscribeNoteEvents()
    {
      if (_eventsSubscribed || _noteController == null) return;

      _noteController.OnNoteMiss += HandleNoteMiss;
      _noteController.OnDragReady += HandleDragReady;
      _noteController.OnActiveHoldEnded += HandleActiveHoldEnded;
      _noteController.OnActiveHoldTick += HandleActiveHoldTick;
      _noteController.OnAutoHit += HandleAutoHit;

      _eventsSubscribed = true;
    }

    public override void _ExitTree()
    {
      base._ExitTree();
      if (_noteController != null && _eventsSubscribed)
      {
        _noteController.OnNoteMiss -= HandleNoteMiss;
        _noteController.OnDragReady -= HandleDragReady;
        _noteController.OnActiveHoldEnded -= HandleActiveHoldEnded;
        _noteController.OnActiveHoldTick -= HandleActiveHoldTick;
        _noteController.OnAutoHit -= HandleAutoHit;
        _eventsSubscribed = false;
      }
    }

    // =============================================
    // Input Entry Points
    // =============================================

    /// <summary>Called when a Tap/Hold key is pressed.</summary>
    public void OnTapKeyPressed()
    {
      _keysHeldCount++;
      if (_audioController == null || _noteController == null) return;

      double currentBeat = _audioController.CurrentBeat;
      ProcessSingleHit(currentBeat);
    }

    /// <summary>Called when Left Shift (Focus) is pressed.</summary>
    public void OnFocusKeyPressed()
    {
      if (_audioController == null || _noteController == null) return;

      double currentBeat = _audioController.CurrentBeat;

      // End focusable state on all currently-focusable windows
      EndAllActiveFocusable(currentBeat);

      // Try to hit Focus notes
      ProcessBroadcastHit(NoteType.Focus, (float)currentBeat);
    }

    /// <summary>Called when Right Shift (Close) is pressed.</summary>
    public void OnCloseKeyPressed()
    {
      if (_audioController == null || _noteController == null) return;

      double currentBeat = _audioController.CurrentBeat;
      ProcessBroadcastHit(NoteType.Close, (float)currentBeat);
    }

    /// <summary>Called when any gameplay key is released.</summary>
    public void OnKeyReleased()
    {
      _keysHeldCount = Mathf.Max(0, _keysHeldCount - 1);

      if (_audioController != null)
        _lastKeyReleaseBeat = _audioController.CurrentBeat;

      // If all keys released, check for early hold releases
      if (_keysHeldCount == 0)
        CheckHoldEarlyRelease();
    }

    // =============================================
    // Per-Frame Processing
    // =============================================

    /// <summary>
    /// Called each frame. Handles drag auto-hit when a key is held
    /// or within Good timing window (125ms) after release.
    /// </summary>
    public bool IsDragActive(double currentBeat)
    {
      if (_keysHeldCount > 0) return true;

      if (_lastKeyReleaseBeat > double.MinValue && _audioController?.Metronome != null)
      {
        double elapsedMs = _audioController.Metronome.ToDeltaMilliSeconds(
          _lastKeyReleaseBeat, currentBeat
        );
        return elapsedMs <= Constants.HitResult.TimmingWindowMs[HitResultType.Good];
      }

      return false;
    }

    // =============================================
    // NoteController Event Handlers
    // =============================================

    /// <summary>Fired by NoteController when a note passes timing window without being hit.</summary>
    private void HandleNoteMiss(string windowId, NoteData note)
    {
      if (!note.IsHittable) return;

      var result = HitResult.Miss(note);
      note.IsEvaluated = true;
      OnMiss?.Invoke(windowId, result);

      // Focus miss → make the window focusable
      if (note.Type == NoteType.Focus && _windowController != null)
      {
        _windowController.AddStartFocusable(windowId, note.StartBeat.AbsoluteValue);
      }

      // Close miss → make the window unresponsive
      if (note.Type == NoteType.Close && _windowController != null)
      {
        _windowController.SetUnresponsive(windowId);
      }
    }

    /// <summary>Fired by NoteController when a Drag note enters judgement zone.</summary>
    private void HandleDragReady(string windowId, NoteData note, double elapsedMs)
    {
      bool dragActive = _noteController.Autoplay || IsDragActive(_audioController.CurrentBeat);
      if (!dragActive) return;

      var result = HitResult.DragHit(note, elapsedMs);
      if (result.IsHit)
      {
        RequestHitFX(windowId, note, result.Type);
        note.IsEvaluated = true;
        _noteController.ConsumeNote(windowId, note);
        OnHit?.Invoke(windowId, result);
        PlayHitSound(note.Type, note.ResourcePack);
      }
    }

    /// <summary>Fired by NoteController each frame for active hold notes.</summary>
    private void HandleActiveHoldTick(string windowId, NoteData note)
    {
      // Hold sustain is checked on key release (CheckHoldEarlyRelease)
      // Nothing to do per-tick here; the hold continues as long as keys are held.
      if (_audioController == null) return;
      if (!TryGetResourcePack(note, out var resourcePack)) return;

      int intervalMs = resourcePack.Config.HitFXHoldTickMs;
      if (intervalMs <= 0) return;

      double activeMs = _audioController.Metronome.ToDeltaMilliSeconds(
        note.StartBeat.AbsoluteValue,
        _audioController.CurrentBeat
      );
      if (activeMs < intervalMs) return;

      long tickIndex = (long)Math.Floor(activeMs / intervalMs);
      if (!_lastHoldTickIndex.TryGetValue(note, out long lastTickIndex))
      {
        _lastHoldTickIndex[note] = 0;
        return;
      }
      if (tickIndex <= lastTickIndex) return;

      _lastHoldTickIndex[note] = tickIndex;
      RequestHitFX(windowId, note, note.HoldStartResult.Type);
    }

    /// <summary>Fired by NoteController when a hold note reaches its tail.</summary>
    private void HandleActiveHoldEnded(string windowId, NoteData note)
    {
      // Hold completed successfully
      note.IsEvaluated = true;
      _lastHoldTickIndex.Remove(note);
      OnHit?.Invoke(windowId, note.HoldStartResult);
    }

    /// <summary>Fired by NoteController for auto-hit (autoplay/ghost notes).</summary>
    private void HandleAutoHit(string windowId, NoteData note)
    {
      if (TryGetResourcePack(note, out var resourcePack))
      {
        RequestHitFX(windowId, note, resourcePack.Config.HitFXAutoResult);
        if (note.Type == NoteType.Hold)
        {
          _lastHoldTickIndex[note] = 0;
          note.HoldStartResult = HitResult.AutoHit(note);
        }
      }

      if (note.Type != NoteType.Hold) _noteController.ConsumeNote(windowId, note);
      PlayHitSound(note.Type, note.ResourcePack);
    }

    // =============================================
    // Hit Processing Logic
    // =============================================

    /// <summary>
    /// Single hit: finds the closest Tap/Hold note across focused windows.
    /// One key press = one note consumed.
    /// </summary>
    private void ProcessSingleHit(double currentBeat)
    {
      var best = FindClosestNote(NoteType.Tap, (float)currentBeat);
      if (!best.HasValue) return;

      string windowId = best.Value.WindowId;
      NoteData note = best.Value.Note;

      double offsetMs = _audioController.Metronome.ToDeltaMilliSeconds(
        note.StartBeat.AbsoluteValue, currentBeat
      );

      var result = HitResult.FromOffset(note, offsetMs);

      if (result.IsHit)
      {
        RequestHitFX(windowId, note, result.Type);
        if (note.Type == NoteType.Hold)
        {
          // track offset, begin hold tracking
          note.HoldStartResult = result;
          SetHoldActive(windowId, note);
          _lastHoldTickIndex[note] = 0;
        }
        else
        {
          // Tap: consume immediately
          note.IsEvaluated = true;
          _noteController.ConsumeNote(windowId, note);
          OnHit?.Invoke(windowId, result);
        }

        PlayHitSound(note.Type, note.ResourcePack);
      }
    }

    /// <summary>
    /// Broadcast hit: evaluates ALL notes of the given type across ALL windows.
    /// Used for Focus and Close notes.
    /// </summary>
    private void ProcessBroadcastHit(NoteType type, float currentBeat)
    {
      var results = TryEvaluateAll(type, currentBeat);
      foreach (var tuple in results)
      {
        var result = HitResult.FromOffset(tuple.Note, tuple.OffsetMs);
        if (result.IsHit)
        {
          RequestHitFX(tuple.WindowId, tuple.Note, result.Type);
          tuple.Note.IsEvaluated = true;
          _noteController.ConsumeNote(tuple.WindowId, tuple.Note);
          OnHit?.Invoke(tuple.WindowId, result);
          PlayHitSound(tuple.Note.Type, tuple.Note.ResourcePack);
        }
      }
    }

    /// <summary>
    /// When all keys are released, check all active holds.
    /// If a hold note is about to end within Good window (125ms), let it complete naturally.
    /// Otherwise, force a miss.
    /// </summary>
    private void CheckHoldEarlyRelease()
    {
      if (_noteController == null || _audioController == null) return;

      double currentBeat = _audioController.CurrentBeat;
      double goodWindowMs = Constants.HitResult.TimmingWindowMs[HitResultType.Good];

      var activeHolds = GetActiveHolds();

      foreach (var (windowId, note) in activeHolds)
      {
        double holdEndBeat = note.StartBeat.AbsoluteValue + note.Length;
        double remainingMs = _audioController.Metronome.ToDeltaMilliSeconds(
          currentBeat, holdEndBeat
        );

        // If the hold is about to end within Good window, let it complete naturally
        if (remainingMs <= goodWindowMs) continue;

        // Early release → miss
        note.IsEvaluated = true;
        note.IsHoldActive = false;
        _lastHoldTickIndex.Remove(note);
        if (note.IsLifecycleBounded)
        {
          var result = HitResult.Miss(note);
          OnMiss?.Invoke(windowId, result);
        }
      }
    }

    /// <summary>
    /// Ends focusable state on all windows that are currently in a focusable period.
    /// Called when Left Shift is pressed regardless of whether Focus notes were hit.
    /// </summary>
    private void EndAllActiveFocusable(double currentBeat)
    {
      if (_windowController == null) return;

      // Iterate all active windows and end focusable state on those currently in a period.
      foreach (var entry in _windowController.GetActiveWindowIds())
      {
        if (_windowController.IsFocusableAt(entry, currentBeat))
        {
          _windowController.AddEndFocusable(entry, currentBeat);
        }
      }
    }

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
      foreach (var entry in _noteController.WindowStates)
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

            double offsetMs = _audioController.Metronome.ToDeltaMilliSeconds(
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

      foreach (var entry in _noteController.WindowStates)
      {
        string windowId = entry.Key;
        NoteController.WindowNoteState state = entry.Value;

        if (state.WindowVisual.UnFocus) continue;

        foreach (var sideEntry in state.WindowData.Notes)
        {
          int cursor = state.EvalCursors[sideEntry.Key];
          var noteList = sideEntry.Value;

          for (int i = cursor; i < noteList.Count; i++)
          {
            NoteData note = noteList[i];
            if (note.IsEvaluated || !note.IsHittable) continue;
            if (note.IsHoldActive) continue;

            bool typeMatches = note.Type == type || (type == NoteType.Tap && note.Type == NoteType.Hold);
            if (!typeMatches) continue;

            double offsetMs = _audioController.Metronome.ToDeltaMilliSeconds(
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
      if (_noteController.WindowStates.TryGetValue(windowId, out var state))
      {
        note.IsHoldActive = true;
        state.ActiveHolds.Add(note);
      }
    }

    /// <summary>Returns all currently active hold notes across all windows.</summary>
    public List<(string WindowId, NoteData Note)> GetActiveHolds()
    {
      _activeHoldsCache.Clear();
      foreach (var entry in _noteController.WindowStates)
      {
        foreach (var holdNote in entry.Value.ActiveHolds)
        {
          if (!holdNote.IsEvaluated)
            _activeHoldsCache.Add((entry.Key, holdNote));
        }
      }
      return _activeHoldsCache;
    }

    // =============================================
    // Audio Playback
    // =============================================

    private void PlayHitSound(NoteType type, ResourcePack? resourcePack = null)
    {
      ResourcePack rp;
      if (resourcePack.HasValue)
        rp = resourcePack.Value;
      else
        rp = ResourcePackManager.Instance.GetActiveResourcePack();

      if (rp.SFX != null && rp.SFX.TryGetValue(type, out var sfx))
      {
        var player = _audioPool.Get();
        player.Stream = sfx;
        player.Play();
      }
    }

    private void RequestHitFX(string windowId, NoteData note, HitResultType resultType)
    {
      if (note == null || note.IsMutedGhost) return;
      if (resultType == HitResultType.Miss) return;
      OnHitFXRequested?.Invoke(windowId, note, resultType);
    }

    private static bool TryGetResourcePack(NoteData note, out ResourcePack resourcePack)
    {
      if (note != null && note.ResourcePack.HasValue)
      {
        resourcePack = note.ResourcePack.Value;
        return true;
      }

      var manager = ResourcePackManager.Instance;
      if (manager != null)
      {
        resourcePack = manager.GetActiveResourcePack();
        return true;
      }

      resourcePack = default;
      return false;
    }
  }
}
