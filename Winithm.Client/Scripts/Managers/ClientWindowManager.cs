using Godot;
using System.Collections.Generic;
using Winithm.Core.Managers;
using Winithm.Core.Data;
using Winithm.Core.Behaviors;
using Winithm.Client.Data;

namespace Winithm.Client.Managers
{
  public class ClientWindowManager : WindowManager
  {
    private NoteManager _noteManager;
    private HitManager _hitManager;
    private ScoreManager _scoreManager;

    // Tracks which windows have been registered with NoteManager
    private HashSet<string> _registeredWindows = new HashSet<string>();

    // Window state overrides
    private Dictionary<string, bool> _unfocusOverride = new Dictionary<string, bool>();
    private Dictionary<string, bool> _focusableOverride = new Dictionary<string, bool>();
    private Dictionary<string, bool> _inputBlocked = new Dictionary<string, bool>();
    private Dictionary<string, bool> _notResponding = new Dictionary<string, bool>();
    private Dictionary<string, float> _notRespondingStartBeat = new Dictionary<string, float>();

    public ScoreManager ScoreManager => _scoreManager;

    public override void _Ready()
    {
      base._Ready();

      // Global NoteManager
      _noteManager = new NoteManager();
      _noteManager.Name = "NoteManager";
      AddChild(_noteManager);
      _noteManager.Init(_timeManager);
      _noteManager.OnNoteMiss += (windowId, note) => _hitManager.RegisterMiss(windowId, note);

      // HitManager
      _hitManager = new HitManager();
      _hitManager.Name = "HitManager";
      AddChild(_hitManager);
      _hitManager.Init(_timeManager, _noteManager);
      _hitManager.OnHit += HandleHit;
      _hitManager.OnMiss += HandleMiss;

      _scoreManager = new ScoreManager();
    }

    public override void _Input(InputEvent @event)
    {
      if (@event is InputEventKey keyEvent && !keyEvent.Echo)
      {
        if (keyEvent.Pressed)
        {
          NoteType inputType = NoteType.Tap;
          if (keyEvent.Scancode == (uint)KeyList.Shift) inputType = NoteType.Focus;
          else if (keyEvent.Scancode == (uint)KeyList.Alt) inputType = NoteType.Close;

          _hitManager.OnKeyPressed(inputType);
        }
        else
        {
          _hitManager.OnKeyReleased();
        }
      }
    }

    public override void _Process(float delta)
    {
      base._Process(delta);

      if (_timeManager == null) return;
      float currentBeat = _timeManager.CurrentBeat;
      float currentTimeMs = _timeManager.CurrentTimeMs;

      // Pass input blocked state
      _hitManager.SetInputBlockedState(_inputBlocked);

      // Register windows with NoteManager when they become active
      foreach (var pair in _activeWindows)
      {
        string id = pair.Key;
        Window w = pair.Value;

        if (!_registeredWindows.Contains(id))
        {
          WindowData wd = _windowDataList.Find(x => x.ID == id);
          if (wd == null) continue;

          CullFakeNotes(wd);
          _noteManager.RegisterWindow(id, wd, w);
          _registeredWindows.Add(id);
        }
      }

      // Auto-evaluate drag notes each frame
      _hitManager.ProcessDragNotes(currentTimeMs, currentBeat);

      // Cleanup inactive windows
      List<string> toRemove = new List<string>();
      foreach (var id in _registeredWindows)
      {
        if (!_activeWindows.ContainsKey(id))
          toRemove.Add(id);
      }
      foreach (var id in toRemove)
      {
        _noteManager.UnregisterWindow(id);
        _registeredWindows.Remove(id);
        _unfocusOverride.Remove(id);
        _focusableOverride.Remove(id);
        _inputBlocked.Remove(id);
        _notResponding.Remove(id);
        _notRespondingStartBeat.Remove(id);
      }
    }

    // =============== Hit/Miss Callbacks ===============

    private void HandleHit(string windowId, HitResult result)
    {
      _scoreManager.RegisterHit(result);

      if (result.Note.Type == NoteType.Focus)
      {
        _unfocusOverride[windowId] = false;
        _focusableOverride[windowId] = false;
        _inputBlocked[windowId] = false;

        foreach (var id in new List<string>(_focusableOverride.Keys))
        {
          if (_focusableOverride.TryGetValue(id, out bool isFocusable) && isFocusable)
          {
            _unfocusOverride[id] = false;
            _focusableOverride[id] = false;
            _inputBlocked[id] = false;
          }
        }
      }
    }

    private void HandleMiss(string windowId, HitResult result)
    {
      _scoreManager.RegisterHit(result); // weight = 0

      if (result.Note.Type == NoteType.Focus)
      {
        _focusableOverride[windowId] = true;
        _inputBlocked[windowId] = true;
      }
      else if (result.Note.Type == NoteType.Close)
      {
        if (!_notResponding.ContainsKey(windowId) || !_notResponding[windowId])
        {
          _notResponding[windowId] = true;
          _notRespondingStartBeat[windowId] = _timeManager?.CurrentBeat ?? 0f;

          WindowData wd = _windowDataList.Find(x => x.ID == windowId);
          if (wd != null)
          {
            float extensionBeats = _timeManager?.Metronome?.GetCurrentBPS(_timeManager.CurrentTime) ?? 2f;
            wd.EndBeat += extensionBeats;
          }
        }
      }
    }

    // =============== FakeType Culling ===============

    private void CullFakeNotes(WindowData wd)
    {
      if (!wd.UnFocus || wd.Notes == null || wd.Notes.Count == 0) return;

      NoteData firstFocusNote = null;
      foreach (var noteList in wd.Notes.Values)
      {
        foreach (var note in noteList)
        {
          if (note.Type == NoteType.Focus) { firstFocusNote = note; break; }
        }
        if (firstFocusNote != null) break;
      }

      foreach (var noteList in wd.Notes.Values)
      {
        foreach (var note in noteList)
        {
          if (note.Type == NoteType.Focus || note.Type == NoteType.Close) continue;
          if (note.FakeType != 0) continue;

          if (firstFocusNote == null)
            note.FakeType = 1;
          else if (note.Start.AbsoluteValue < firstFocusNote.Start.AbsoluteValue)
            note.FakeType = 1;
        }
      }
    }

    // =============== ApplyFlags ===============

    protected override void ApplyFlags(Window w, WindowData wd, float currentBeat)
    {
      base.ApplyFlags(w, wd, currentBeat);

      // Focus overlay: unfocused windows get a dark overlay with pulsing when focusable
      if (_unfocusOverride.TryGetValue(wd.ID, out bool unfocus) && unfocus)
      {
        // Base unfocus: dark overlay
        w.FocusOverlayOpacity = 0.45f;

        // Focusable: pulse white on top
        if (_focusableOverride.TryGetValue(wd.ID, out bool focusable) && focusable)
          w.FocusOverlayOpacity = 0.45f + Mathf.Abs(Mathf.Sin(currentBeat * Mathf.Pi * 2f)) * 0.3f;
      }
      else
      {
        w.FocusOverlayOpacity = 0f;
      }

      // Not Responding state
      if (_notResponding.TryGetValue(wd.ID, out bool nr) && nr
          && _notRespondingStartBeat.TryGetValue(wd.ID, out float startBeat))
      {
        w.IsNotRespondingTitle = true;

        float durationBeats = _timeManager?.Metronome?.GetCurrentBPS(_timeManager.CurrentTime) ?? 2f;
        float elapsed = currentBeat - startBeat;
        float t = Mathf.Clamp(elapsed / durationBeats, 0f, 1f);
        w.UnresponsiveOverlayOpacity = t;
      }
      else
      {
        w.IsNotRespondingTitle = false;
        w.UnresponsiveOverlayOpacity = 0f;
      }
    }
  }
}
