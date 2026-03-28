using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Managers;
using Winithm.Client.Data;

namespace Winithm.Client.Managers
{
  /// <summary>
  /// Central input evaluator for the gameplay.
  /// Routes key presses to the global NoteManager and evaluates timing.
  /// 
  /// Input rules:
  /// - Tap/Hold: AnyKey. N notes at judgement = N presses needed simultaneously.
  /// - Drag: AnyKey held OR within 200ms grace after release → auto hit.
  /// - Focus (Left Shift): One press resolves ALL Focus notes across ALL windows.
  /// - Close (Right Shift): One press resolves ALL Close notes across ALL windows.
  /// </summary>
  public class HitManager : Node
  {
    public event Action<string, HitResult> OnHit;  // windowId, result
    public event Action<string, HitResult> OnMiss;  // windowId, result

    private TimeManager _timeManager;
    private NoteManager _noteManager;
    private Dictionary<string, bool> _inputBlocked;

    // Drag grace: track when the last key was released
    private float _lastKeyReleaseTimeMs = -1000f;
    // Track how many keys are currently held down
    private int _keysHeldCount = 0;

    // Hit window constant: notes beyond 200ms get no reaction at all
    private const float MaxHitWindowMs = 200f;

    public void Init(TimeManager tm, NoteManager nm)
    {
      _timeManager = tm;
      _noteManager = nm;
    }

    public void SetInputBlockedState(Dictionary<string, bool> inputBlocked)
    {
      _inputBlocked = inputBlocked;
    }

    /// <summary>
    /// Called from ClientWindowManager._Input when a key is pressed.
    /// </summary>
    public void OnKeyPressed(NoteType inputType)
    {
      if (_timeManager == null || _noteManager == null) return;

      float currentBeat = _timeManager.CurrentBeat;

      switch (inputType)
      {
        case NoteType.Focus:
          ProcessBroadcastHit(NoteType.Focus, currentBeat);
          break;

        case NoteType.Close:
          ProcessBroadcastHit(NoteType.Close, currentBeat);
          break;

        case NoteType.Tap:
        default:
          _keysHeldCount++;
          ProcessSingleHit(currentBeat);
          break;
      }
    }

    /// <summary>
    /// Called when a key is released. Tracks drag grace period.
    /// </summary>
    public void OnKeyReleased()
    {
      _keysHeldCount = Mathf.Max(0, _keysHeldCount - 1);
      _lastKeyReleaseTimeMs = _timeManager?.CurrentTimeMs ?? 0f;
    }

    /// <summary>
    /// Called each frame to auto-evaluate Drag notes while a key is held
    /// or within the 200ms grace period.
    /// </summary>
    public void ProcessDragNotes(float currentTimeMs, float currentBeat)
    {
      if (_noteManager == null) return;

      bool dragActive = _keysHeldCount > 0
                        || (currentTimeMs - _lastKeyReleaseTimeMs) <= 200f;

      if (!dragActive) return;

      var dragResult = _noteManager.TryEvaluateDrag(currentBeat);
      if (dragResult.HasValue)
      {
        // Check if window is blocked
        if (_inputBlocked != null
            && _inputBlocked.TryGetValue(dragResult.Value.WindowId, out bool blocked) && blocked)
          return;

        var result = HitResult.DragHit(dragResult.Value.Note, dragResult.Value.OffsetMs);
        if (result.IsHit)
        {
          _noteManager.ConsumeNote(dragResult.Value.WindowId, dragResult.Value.Note);
          OnHit?.Invoke(dragResult.Value.WindowId, result);
        }
      }
    }

    /// <summary>
    /// Called when a note is missed (passed the judgement line without being hit).
    /// </summary>
    public void RegisterMiss(string windowId, NoteData note)
    {
      var result = HitResult.Miss(note);
      OnMiss?.Invoke(windowId, result);
    }

    // ====== Private helpers ======

    /// <summary>
    /// Broadcast hit: evaluates ALL notes of the given type across ALL windows.
    /// Used for Focus and Close notes.
    /// </summary>
    private void ProcessBroadcastHit(NoteType type, float currentBeat)
    {
      var results = _noteManager.TryEvaluateAll(type, currentBeat, MaxHitWindowMs);
      foreach (var tuple in results)
      {
        var result = HitResult.FromOffset(tuple.Note, tuple.OffsetMs);
        if (result.IsHit)
        {
          _noteManager.ConsumeNote(tuple.WindowId, tuple.Note);
          OnHit?.Invoke(tuple.WindowId, result);
        }
      }
    }

    /// <summary>
    /// Single hit: finds the closest Tap/Hold note across all windows and consumes it.
    /// One key press = one note consumed.
    /// </summary>
    private void ProcessSingleHit(float currentBeat)
    {
      var best = _noteManager.FindClosestNote(NoteType.Tap, currentBeat, MaxHitWindowMs, _inputBlocked);
      if (!best.HasValue) return;

      string windowId = best.Value.WindowId;
      NoteData note = best.Value.Note;

      float offsetMs = BeatToMs(note.Start.AbsoluteValue - currentBeat);
      var result = HitResult.FromOffset(note, offsetMs);

      if (result.IsHit)
      {
        if (note.Type == NoteType.Hold)
        {
          note.IsEvaluated = true;
          OnHit?.Invoke(windowId, result);
        }
        else
        {
          _noteManager.ConsumeNote(windowId, note);
          OnHit?.Invoke(windowId, result);
        }
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
