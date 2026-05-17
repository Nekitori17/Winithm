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
    // Rack to add controller
    private Node _controllerRack;

    // Core controllers
    private AudioController _audioController;
    private ComponentController _componentController;
    private NoteController _noteController;
    private WindowController _windowController;
    private GroupController _groupController;
    private ThemeChannelController _themeController;

    // Client controllers
    private HitController _hitController;
    private ScoreController _scoreController;

    // Data
    private ChartData _chartData;

    // Playfield node from scene tree
    private Control _playfield;


    private Label _debug;

    public static readonly string LEVEL_DIR = "res://Winithm.Assets/Levels";

    public override void _Ready()
    {
      _playfield = GetNode<Control>("Playfield");
      _controllerRack = GetNode<Node>("ControllerRack");
      _componentController = GetNode<ComponentController>("ScoreUI");

      _debug = GetNode<Label>("Debug");

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
      _noteController.Initialize(metronome);
      _windowController.Initialize(
        _playfield, _chartData.Windows, metronome,
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

    public override void _Process(float delta)
    {
      if (_audioController == null || !_audioController.IsPlaying) return;

      // Tick the master clock
      _audioController.Tick(delta);

      double currentBeat = _audioController.CurrentBeat;

      _debug.Text = $"Beat: {currentBeat:F2}" + "\n" + $"FPS: {Engine.GetFramesPerSecond()}";

      // Update all controllers in order
      _windowController.ScreenSize = OS.GetScreenSize();
      _windowController.PlayerAreaSize = _playfield.RectSize;
      _windowController.Update(currentBeat);

      _noteController.Update(currentBeat);

      double length = _audioController.Length;
      _componentController.SongProgressPercent = length > 0 ? (float)(_audioController.CurrentTime / length) : 0f;

      _componentController.ScreenSize = OS.WindowSize;

      _componentController.SetCombo(_scoreController.GetCurrentCombo());
      _componentController.SetScore(_scoreController.GetRealtimeScore());
      _componentController.SetAccuracy(_scoreController.GetRealtimeAccuracy());
      _componentController.SetStatus(_scoreController.GetStatus());

      _componentController.Update(currentBeat);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
      if (_hitController == null || _audioController == null || !_audioController.IsPlaying) return;

      if (@event is InputEventKey keyEvent)
      {
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
