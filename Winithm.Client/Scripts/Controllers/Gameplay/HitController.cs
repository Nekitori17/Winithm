using Godot;
using System;
using Winithm.Core.Controllers;
using Winithm.Core.Data;
using Winithm.Core.Managers;

using HitResultConstants = Winithm.Core.Constants.HitResult;

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

    private AudioController _audioController;
    private NoteController _noteController;
    private WindowController _windowController;

    private int _keysHeldCount = 0;
    private double _lastKeyReleaseBeat = double.MinValue;

    // Cache to avoid re-registering event handlers
    private bool _eventsSubscribed = false;

    // Object pool for polyphonic hit sounds
    private NodePool<AudioStreamPlayer> _audioPool;

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
        return elapsedMs <= HitResultConstants.TimmingWindowMs[HitResultType.Good];
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
      if (!IsDragActive(_audioController.CurrentBeat)) return;

      var result = HitResult.DragHit(note, elapsedMs);
      if (result.IsHit)
      {
        note.IsEvaluated = true;
        _noteController.ConsumeNote(windowId, note);
        OnHit?.Invoke(windowId, result);
        PlayHitSound(note.Type);
      }
    }

    /// <summary>Fired by NoteController each frame for active hold notes.</summary>
    private void HandleActiveHoldTick(string windowId, NoteData note)
    {
      // Hold sustain is checked on key release (CheckHoldEarlyRelease)
      // Nothing to do per-tick here; the hold continues as long as keys are held.
    }

    /// <summary>Fired by NoteController when a hold note reaches its tail.</summary>
    private void HandleActiveHoldEnded(string windowId, NoteData note)
    {
      // Hold completed successfully
      note.IsEvaluated = true;
      var result = HitResult.FromOffset(note, note.HoldStartOffsetMs);
      OnHit?.Invoke(windowId, result);
      PlayHitSound(note.Type);
    }

    /// <summary>Fired by NoteController for auto-hit (autoplay/ghost notes).</summary>
    private void HandleAutoHit(string windowId, NoteData note)
    {
      if (note.Type == NoteType.Hold && note.IsHittable)
      {
        note.HoldStartOffsetMs = 0;
      } else if (note.IsHittable)
      {
        note.IsEvaluated = true;
        _noteController.ConsumeNote(windowId, note);
        OnHit?.Invoke(windowId, HitResult.FromOffset(note, 0));
      }
      PlayHitSound(note.Type);
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
      var best = _noteController.FindClosestNote(NoteType.Tap, (float)currentBeat);
      if (!best.HasValue) return;

      string windowId = best.Value.WindowId;
      NoteData note = best.Value.Note;

      double offsetMs = _audioController.Metronome.ToDeltaMilliSeconds(
        note.StartBeat.AbsoluteValue, currentBeat
      );

      var result = HitResult.FromOffset(note, offsetMs);

      if (result.IsHit)
      {
        if (note.Type == NoteType.Hold)
        {
          // track offset, begin hold tracking
          note.HoldStartOffsetMs = offsetMs;
          _noteController.SetHoldActive(windowId, note);
        }
        else
        {
          // Tap: consume immediately
          note.IsEvaluated = true;
          _noteController.ConsumeNote(windowId, note);
          OnHit?.Invoke(windowId, result);
        }

        PlayHitSound(note.Type);
      }
    }

    /// <summary>
    /// Broadcast hit: evaluates ALL notes of the given type across ALL windows.
    /// Used for Focus and Close notes.
    /// </summary>
    private void ProcessBroadcastHit(NoteType type, float currentBeat)
    {
      var results = _noteController.TryEvaluateAll(type, currentBeat);
      foreach (var tuple in results)
      {
        var result = HitResult.FromOffset(tuple.Note, tuple.OffsetMs);
        if (result.IsHit)
        {
          tuple.Note.IsEvaluated = true;
          _noteController.ConsumeNote(tuple.WindowId, tuple.Note);
          OnHit?.Invoke(tuple.WindowId, result);
          PlayHitSound(tuple.Note.Type);
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
      double goodWindowMs = HitResultConstants.TimmingWindowMs[HitResultType.Good];

      var activeHolds = _noteController.GetActiveHolds();

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
        var result = HitResult.Miss(note);
        OnMiss?.Invoke(windowId, result);
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
    // Audio Playback
    // =============================================

    private void PlayHitSound(NoteType type)
    {
      var inst = ResourcePackManager.Instance;
      if (inst == null || _audioPool == null) return;
      
      var pack = inst.GetActiveResourcePack();
      if (pack.SFX != null && pack.SFX.TryGetValue(type, out var sfx))
      {
        var player = _audioPool.Get();
        player.Stream = sfx;
        player.Play();
      }
    }
  }
}
