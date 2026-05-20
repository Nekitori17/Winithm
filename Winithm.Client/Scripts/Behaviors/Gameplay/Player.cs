using Godot;
using Newtonsoft.Json;
using System;
using Winithm.Core.Common;
using Winithm.Core.Controllers;
using Winithm.Core.Data;
using Winithm.Core.Managers;
using Winithm.Client.Controllers.Gameplay;
using Winithm.Core.Behaviors.ScoreUI;

namespace Winithm.Client.Behaviors.Gameplay
{
  /// <summary>
  /// Main gameplay orchestrator. Creates and wires all Core controllers,
  /// drives the game loop, and routes input to HitController.
  /// </summary>
  public class Player : Control
  {
    [Export] public bool Autoplay = false;
    [Export] public float NoteSize = 1f;
    [Export] public float NoteSpeed = 1f;

    public readonly float PAUSE_BACK_TIME_SECS = 3f;
    public readonly float REWIND_TIME_SECS = 0.5f;

    // Rack to add controller
    private Node _controllerRack;

    // Core controllers
    private AudioController _audioController;
    private ComponentController _componentController;
    private NoteController _noteController;
    private WindowController _windowController;
    private HitFXController _hitFXController;
    private GroupController _groupController;
    private ThemeChannelController _themeController;

    // Client controllers
    private HitController _hitController;
    private ScoreController _scoreController;

    // Data
    private ChartData _chartData;

    // Playfield node from scene tree
    private Control _playfield;
    private Control _objectsLayer;
    private Control _hitFXLayer;

    private bool _isPaused = false;
    private float _rewindTimeSecs = 0.5f;

    private Label _debug;

    public static readonly string LEVEL_DIR = "res://Winithm.Assets/Levels";

    public override void _Ready()
    {
      _playfield = GetNode<Control>("Playfield");
      _objectsLayer = GetNode<Control>("Playfield/ObjectsLayer");
      _hitFXLayer = GetNode<Control>("Playfield/HitFXLayer");

      _controllerRack = GetNode<Node>("ControllerRack");
      _componentController = GetNode<ComponentController>("ScoreUI");

      _debug = GetNode<Label>("Debug");

      _rewindTimeSecs = REWIND_TIME_SECS;

      SetAutoPlay(true);
      SetNoteSize(1.3f);
      SetNoteSpeed(7.5f);

      InitializeControllers();
      LoadDemoLevel();
    }

    private void InitializeControllers()
    {
      // Audio controller (master clock)
      _audioController = new AudioController() { Name = "AudioController" };
      _controllerRack.AddChild(_audioController);

      // Group controller
      _groupController = new GroupController() { Name = "GroupController" };
      _controllerRack.AddChild(_groupController);

      // Theme channel controller
      _themeController = new ThemeChannelController() { Name = "ThemeChannelController" };
      _controllerRack.AddChild(_themeController);

      // Note controller (rendering + lifecycle)
      _noteController = new NoteController() { Name = "NoteController" };
      _controllerRack.AddChild(_noteController);

      // Hit FX controller (visual-only effects)
      _hitFXController = new HitFXController() { Name = "HitFXController" };
      _controllerRack.AddChild(_hitFXController);

      // Window controller (window rendering + lifecycle)
      _windowController = new WindowController() { Name = "WindowController" };
      _controllerRack.AddChild(_windowController);

      // Hit controller (input evaluation)
      _hitController = new HitController() { Name = "HitController" };
      _controllerRack.AddChild(_hitController);

      // Score controller (plain class, no Node)
      _scoreController = new ScoreController();

      // Wire score events
      _hitController.OnHit += (windowId, result) => _scoreController.RegisterHit(result);
      _hitController.OnMiss += (windowId, result) => _scoreController.RegisterHit(result);
      _hitController.OnHitFXRequested += (windowId, note, resultType) =>
        _hitFXController.RequestHitFX(windowId, note, resultType);
    }

    private async void LoadDemoLevel() => LoadLevel(
      "frizka.allMyFellas",
      "info"
    );

    public async void LoadLevel(
      string songID,
      string chartID
    )
    {

      _chartData = WinithmIO.LoadLevel(LEVEL_DIR, songID, chartID);
      if (_chartData == null)
      {
        GD.PrintErr("[Player] Failed to load level data.");
        return;
      }

      // Initialize audio
      var metronome = _chartData.SongMetaData.Audio.Metronome;
      _audioController.Initialize(metronome);

      // Use the stream already loaded by WNMParser
      if (_chartData.SongMetaData.Audio.SongStream != null)
      {
        _audioController.SetStream(_chartData.SongMetaData.Audio.SongStream);
      }

      // Initialize controllers with shared dependencies
      _groupController.Initialize(_chartData.Groups);
      _themeController.Initialize(_chartData.ThemeChannels);

      _noteController.Initialize(metronome, Autoplay);
      _noteController.PlayerNoteSize = NoteSize;
      _noteController.PlayerNoteSpeed = NoteSpeed;
      _hitFXController.Initialize(_hitFXLayer, _noteController);
      
      foreach (var pack in ResourcePackManager.Instance.GetAllResourcePacks())
      {
        _hitFXController.Prewarm(pack);
      }

      _windowController.Initialize(
        _objectsLayer, _chartData.Windows, metronome,
        _groupController, _themeController, _noteController
      );
      _componentController.Initialize(
        _chartData.Components, metronome,
        _chartData.SongMetaData, _chartData.ChartMetadata
      );

      _hitController.Initialize(_audioController, _noteController, _windowController);

      // Set total hittable notes for scoring
      _scoreController.SetTotalCombos(_chartData.Windows.TotalComboCount);
      
      _componentController.SetAccuracy(1f);
      _componentController.SetScore(0);
      _componentController.SetCombo(0);
      if (Autoplay)
        _componentController.SetStatus(PlayerCombo.Status.AT);
      else
        _componentController.SetStatus(PlayerCombo.Status.AP);

      // Apply initial sizing
      ApplyScreenSize();

      // Connect parent resize
      var parent = GetParent<Control>();
      if (parent != null)
        parent.Connect("item_rect_changed", this, nameof(OnParentResized));

      // Delay to ensure Godot splash screen and rendering initialize cleanly.
      await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

      _audioController.Resume();
    }

    public void SetAutoPlay(bool active) => Autoplay = active; 
    public void SetNoteSize(float size) => NoteSize = size; 
    public void SetNoteSpeed(float speed) => NoteSpeed = speed; 

    public override void _Process(float delta)
    {
      if (_audioController == null) return;
      if (!_audioController.IsPlaying && !_isPaused) return;

      if (_isPaused)
      {
        if (_rewindTimeSecs <= 0) return; // Rewind done, freeze visuals

        _rewindTimeSecs -= delta;
        if (_rewindTimeSecs < 0) _rewindTimeSecs = 0;

        float calcDelta = delta * (PAUSE_BACK_TIME_SECS / REWIND_TIME_SECS);
        _audioController.AdjustTime(-calcDelta);
      }
      else
      {
        // Tick the master clock
        _audioController.Tick(delta);
      }

      double currentBeat = _audioController.CurrentBeat;

      _debug.Text = 
        $"Beat: {currentBeat:F2}" + "\n" 
        + $"FPS: {Engine.GetFramesPerSecond()} | Train: {delta * 1000:F2}ms | Vsync: {(OS.VsyncEnabled ? "On" : "Off")}";

      // Update all controllers in order
      _windowController.ScreenSize = OS.GetScreenSize();
      _windowController.PlayerAreaSize = _playfield.RectSize;
      _windowController.Update(currentBeat);

      _noteController.Update(currentBeat);
      double length = _audioController.Length;
      _componentController.SongProgressPercent = length > 0 ? (float)(_audioController.CurrentTime / length) : 0f;

      _componentController.ScreenSize = OS.WindowSize;

      if (Autoplay)
      {
        int totalComboPassed = 
        _noteController.GetTotalComboPassedInActivingWindows(currentBeat)
        +
        _windowController.GetTotalComboPassedInDestroyedWindows(currentBeat);

        _scoreController.SetWeightGained(totalComboPassed);
        _scoreController.SetComboEvaluated(totalComboPassed);

        _componentController.SetCombo(totalComboPassed);
        _componentController.SetScore(_scoreController.GetRealtimeScore());
        _componentController.SetAccuracy(_scoreController.GetRealtimeAccuracy());
        _componentController.SetStatus(PlayerCombo.Status.AT);
      } else
      {
        _componentController.SetCombo(_scoreController.GetCurrentCombo());
        _componentController.SetScore(_scoreController.GetRealtimeScore());
        _componentController.SetAccuracy(_scoreController.GetRealtimeAccuracy());
        _componentController.SetStatus(_scoreController.GetStatus());
      }

      _componentController.Update(currentBeat);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
      if (_hitController == null || _audioController == null) return;

      if (@event is InputEventKey keyEvent)
      {
        // PauseKey must be handled before Autoplay and IsPlaying guards,
        // so pause/resume always works regardless of game state.
        if (keyEvent.Pressed && !keyEvent.Echo && @event.IsAction("PauseKey"))
        {
          if (!_isPaused && _audioController.IsPlaying)
          {
            _isPaused = true;
            _rewindTimeSecs = REWIND_TIME_SECS;
            _audioController.Pause();
            _componentController.DrainPauseBar();
          }
          else if (_isPaused && _rewindTimeSecs <= 0f)
          {
            // Only allow resume after rewind animation completes
            _isPaused = false;
            _audioController.Resume();
            _componentController.FillPauseBar();
          }
          return;
        }

        // Block all gameplay input when autoplay, paused, or not playing
        if (Autoplay || !_audioController.IsPlaying || _isPaused) return;

        if (keyEvent.Pressed && !keyEvent.Echo)
        {
          if (@event.IsAction("FocusNoteKey"))
          {
            _hitController.OnFocusKeyPressed();
          }
          else if (@event.IsAction("CloseNoteKey"))
          {
            _hitController.OnCloseKeyPressed();
          }
          else
          {
            _hitController.OnTapKeyPressed();
          }
        }
        else if (!keyEvent.Pressed)
        {
          // Only track releases for gameplay keys (not Focus/Close keys)
          if (!@event.IsAction("FocusNoteKey") && !@event.IsAction("CloseNoteKey"))
          {
            _hitController.OnKeyReleased();
          }
        }
      }
    }

    private void OnParentResized()
    {
      ApplyScreenSize();
    }

    private void ApplyScreenSize()
    {
      if (_windowController == null) return;

      _windowController.ScreenSize = OS.WindowSize;

      var parent = GetParent<Control>();
      if (parent != null)
        _windowController.PlayerAreaSize = parent.RectSize;
      else
        _windowController.PlayerAreaSize = RectSize;
    }
  }
}
