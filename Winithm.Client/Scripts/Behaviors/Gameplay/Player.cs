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
  /// Main gameplay orchestrator. Creates and wires all core controllers,
  /// drives the game loop, and routes input to HitController.
  /// </summary>
  public class Player : Control
  {
    // ── Exports ─────────────────────────────────────────────────────────────────

    [Export] public bool Autoplay = false;
    [Export] public float NoteSize = 1f;
    [Export] public float NoteSpeed = 1f;
    [Export] public bool NoteHighLightSimulation = false;

    // ── Pause / rewind constants ─────────────────────────────────────────────────

    /// <summary>How far back (in chart seconds) a pause rewinds the clock.</summary>
    public readonly float PAUSE_REWIND_SECS = 3f;

    /// <summary>Wall-clock duration (seconds) of the rewind animation.</summary>
    public readonly float REWIND_DURATION_SECS = 0.5f;

    // ── Scene nodes ──────────────────────────────────────────────────────────────

    private Node _controllerRack;
    private Control _objectsLayer;
    private Control _hitFXLayer;
    private Label _debug;

    // ── Core controllers ─────────────────────────────────────────────────────────

    private AudioController _audioController;
    private ComponentController _componentController;
    private NoteController _noteController;
    private WindowController _windowController;
    private HitFXController _hitFXController;
    private GroupController _groupController;
    private ThemeChannelController _themeController;

    // ── Client controllers ───────────────────────────────────────────────────────

    private HitController _hitController;
    private ScoreTracker _scoreTracker;

    // ── Data ─────────────────────────────────────────────────────────────────────

    private ChartData _chartData;

    // ── Pause / rewind state machine ─────────────────────────────────────────────

    // Phases:
    //   Idle      – normal playback
    //   Rewinding – clock is being pushed backward in real time
    //   Recovering – clock is advancing back toward the saved position (after unpause)

    private enum PausePhase { Idle, Rewinding, Recovering }

    private PausePhase _pausePhase = PausePhase.Idle;

    // Chart-time position at the moment Pause was pressed.
    private double _timeAtPause = 0d;

    // Target chart-time for the rewind end (may be 0 if pause was very early).
    private double _rewindTarget = 0d;

    // Actual rewind distance: timeAtPause - rewindTarget (≤ PAUSE_REWIND_SECS).
    private double _rewindDistance = 0d;

    // Wall-clock seconds remaining in the current rewind animation.
    private float _rewindTimeLeft = 0f;

    // Rate at which the clock moves during the rewind animation (chart-secs / wall-sec).
    private double _rewindRate = 0d;

    // Cooldown to prevent instant re-pause right after an unpause.
    private float _pauseCooldown = 0f;

    // ── Misc ─────────────────────────────────────────────────────────────────────

    public static readonly string LEVEL_DIR = "res://Winithm.Assets/Levels";

    // ── Godot lifecycle ──────────────────────────────────────────────────────────

    public override void _Ready()
    {
      _objectsLayer = GetNode<Control>("ObjectsLayer");
      _hitFXLayer = GetNode<Control>("HitFXLayer");
      _controllerRack = GetNode<Node>("ControllerRack");
      _componentController = GetNode<ComponentController>("ScoreUI");
      _debug = GetNode<Label>("Debug");

      SetAutoPlay(true);
      SetNoteSize(1.5f);
      SetNoteSpeed(10f);
      SetNoteHighLightSimulation(true);

      InitializeControllers();
      LoadDemoLevel();
    }

    public override void _Process(float delta)
    {
      if (_audioController == null) return;

      // Decrement pause cooldown.
      if (_pauseCooldown > 0f)
        _pauseCooldown -= delta;

      _TickClock(delta);

      // ── Per-frame gameplay updates ────────────────────────────────────────────

      double currentBeat = _audioController.CurrentBeat;

      _debug.Text =
        $"Beat: {currentBeat:F2}\n"
        + $"FPS: {Engine.GetFramesPerSecond()} | Frame: {delta * 1000:F2}ms | Vsync: {(OS.VsyncEnabled ? "On" : "Off")}";

      _windowController.ScreenSize = OS.GetScreenSize();
      _windowController.PlayerAreaSize = RectSize;
      _windowController.Update(currentBeat);

      _noteController.Update(currentBeat);
      _noteController.SetNoteHighlightSimulation(NoteHighLightSimulation);

      double length = _audioController.LevelLength;
      _componentController.SongProgressPercent =
        length > 0 ? (float)(_audioController.CurrentTime / length) : 0f;

      _componentController.ScreenSize = OS.WindowSize;

      _UpdateScore(currentBeat);
      _componentController.Update(currentBeat);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
      if (_hitController == null || _audioController == null) return;
      if (!(@event is InputEventKey keyEvent)) return;
      if (keyEvent.Echo) return;

      if (@event.IsAction("PauseKey"))
      {
        _HandlePauseInput();
        return;
      }

      // Block gameplay input while autoplay, paused/rewinding/recovering, or audio stopped.
      if (Autoplay || _pausePhase != PausePhase.Idle || !_audioController.IsPlaying) return;

      if (@event.IsAction("FocusNoteKey"))
        _hitController.OnFocusKeyPressed();
      else if (@event.IsAction("CloseNoteKey"))
        _hitController.OnCloseKeyPressed();
      else if (!keyEvent.Pressed)
        _OnGameplayKeyReleased(@event);
      else
        _hitController.OnTapKeyPressed();
    }

    // ── Key release routing ──────────────────────────────────────────────────────

    // Called from _UnhandledInput for key-up events on gameplay keys.
    private void _OnGameplayKeyReleased(InputEvent @event)
    {
      if (@event.IsAction("FocusNoteKey") || @event.IsAction("CloseNoteKey")) return;
      _hitController.OnKeyReleased();
    }

    // ── Clock tick logic ─────────────────────────────────────────────────────────

    private void _TickClock(float delta)
    {
      switch (_pausePhase)
      {
        case PausePhase.Rewinding:
          _TickRewind(delta);
          break;

        case PausePhase.Recovering:
          _TickRecover(delta);
          break;

        default:
          _audioController.Tick(delta);
          break;
      }
    }

    /// <summary>
    /// Pushes the paused clock backward at <see cref="_rewindRate"/> until the
    /// animation timer expires or the rewind target is reached.
    /// </summary>
    private void _TickRewind(float delta)
    {
      _rewindTimeLeft -= delta;

      double step = _rewindRate * delta; // chart-seconds to move back this frame

      // Check whether the next step would overshoot the target.
      double distanceLeft = _audioController.CurrentTime - _rewindTarget;
      if (step >= distanceLeft || _rewindTimeLeft <= 0f)
      {
        // Snap to target and freeze — wait for the player to unpause.
        _audioController.AdjustTime(-distanceLeft);
        _rewindTimeLeft = 0f;
      }
      else
      {
        _audioController.AdjustTime(-step);
      }
    }

    /// <summary>
    /// Lets the audio clock run forward normally until it reaches
    /// <see cref="_timeAtPause"/>, at which point recovery ends.
    /// </summary>
    private void _TickRecover(float delta)
    {
      _audioController.Tick(delta);

      if (_audioController.CurrentTime >= _timeAtPause)
      {
        // Snap exactly to pause point and finish recovery.
        _audioController.SeekSeconds(_timeAtPause);
        _pausePhase = PausePhase.Idle;
      }
    }

    // ── Pause input handler ──────────────────────────────────────────────────────

    private void _HandlePauseInput()
    {
      switch (_pausePhase)
      {
        case PausePhase.Idle:
          _BeginPause();
          break;

        case PausePhase.Rewinding:
          // Block unpause while the rewind animation is still playing.
          break;

        case PausePhase.Recovering:
          // Block re-pause during recovery to avoid abuse.
          break;
      }

      // Unpause: rewind must have fully settled (timeLeft == 0) and cooldown expired.
      // This is evaluated only after the switch so the Rewinding case above takes priority.
      if (_pausePhase == PausePhase.Rewinding && _rewindTimeLeft <= 0f)
      {
        if (_pauseCooldown <= 0f)
          _BeginRecover();
      }
    }

    // ── Pause / recover transitions ──────────────────────────────────────────────

    /// <summary>
    /// Captures the current position, computes the rewind target and animation
    /// rate, then begins the rewind phase.
    /// </summary>
    private void _BeginPause()
    {
      _timeAtPause = _audioController.CurrentTime;
      _audioController.Pause();

      // How far back we want to go (clamped so we never go below 0).
      _rewindTarget = Math.Max(0d, _timeAtPause - PAUSE_REWIND_SECS);
      _rewindDistance = _timeAtPause - _rewindTarget; // actual distance ≤ PAUSE_REWIND_SECS

      // Scale animation duration proportionally when we can't go back the full amount.
      // Full distance → REWIND_DURATION_SECS; shorter → proportionally less time.
      float animDuration = (float)(_rewindDistance / PAUSE_REWIND_SECS) * REWIND_DURATION_SECS;
      _rewindTimeLeft = animDuration;

      // Speed of the rewind animation in chart-seconds per wall-second.
      _rewindRate = _rewindDistance > 0d ? _rewindDistance / animDuration : 0d;

      _pausePhase = PausePhase.Rewinding;
      _componentController.DrainPauseBar();
    }

    /// <summary>
    /// Starts recovery: resumes audio from the rewind position so the clock
    /// advances naturally back to <see cref="_timeAtPause"/>.
    /// </summary>
    private void _BeginRecover()
    {
      _pausePhase = PausePhase.Recovering;
      _audioController.Resume();
      _componentController.FillPauseBar();

      // Apply cooldown equal to recovery duration (= rewind distance at 1× speed).
      _pauseCooldown = (float)_rewindDistance;
    }

    // ── Score update ─────────────────────────────────────────────────────────────

    private void _UpdateScore(double currentBeat)
    {
      if (Autoplay)
      {
        int passed =
          _noteController.GetTotalComboPassedInActivingWindows(currentBeat)
          + _windowController.GetTotalComboPassedInDestroyedWindows(currentBeat);

        _scoreTracker.SetWeightGained(passed);
        _scoreTracker.SetComboEvaluated(passed);

        _componentController.SetCombo(passed);
        _componentController.SetScore(_scoreTracker.GetRealtimeScore());
        _componentController.SetAccuracy(_scoreTracker.GetRealtimeAccuracy());
        _componentController.SetStatus(PlayerCombo.Status.AT);
      }
      else
      {
        _componentController.SetCombo(_scoreTracker.GetCurrentCombo());
        _componentController.SetScore(_scoreTracker.GetRealtimeScore());
        _componentController.SetAccuracy(_scoreTracker.GetRealtimeAccuracy());
        _componentController.SetStatus(_scoreTracker.GetStatus());
      }
    }

    // ── Controller initialisation ────────────────────────────────────────────────

    private void InitializeControllers()
    {
      _audioController = new AudioController() { Name = "AudioController" };
      _controllerRack.AddChild(_audioController);

      _groupController = new GroupController() { Name = "GroupController" };
      _controllerRack.AddChild(_groupController);

      _themeController = new ThemeChannelController() { Name = "ThemeChannelController" };
      _controllerRack.AddChild(_themeController);

      _noteController = new NoteController() { Name = "NoteController" };
      _controllerRack.AddChild(_noteController);

      _hitFXController = new HitFXController() { Name = "HitFXController" };
      _controllerRack.AddChild(_hitFXController);

      _windowController = new WindowController() { Name = "WindowController" };
      _controllerRack.AddChild(_windowController);

      _hitController = new HitController() { Name = "HitController" };
      _controllerRack.AddChild(_hitController);

      _scoreTracker = new ScoreTracker();

      // Wire scoring and hit-FX events.
      _hitController.OnHit += (_, result) => _scoreTracker.RegisterHit(result);
      _hitController.OnMiss += (_, result) => _scoreTracker.RegisterHit(result);
      _hitController.OnHitFXRequested += (windowId, note, resultType) =>
        _hitFXController.RequestHitFX(windowId, note, resultType);
    }

    // ── Level loading ─────────────────────────────────────────────────────────────

    private async void LoadDemoLevel() => LoadLevel("frizka.allMyFellas", "info");

    public async void LoadLevel(string songID, string chartID)
    {
      _chartData = WinithmIO.LoadLevel(LEVEL_DIR, songID, chartID);
      if (_chartData == null)
      {
        GD.PrintErr("[Player] Failed to load level data.");
        return;
      }

      var metronome = _chartData.SongMetaData.Audio.Metronome;
      _audioController.Initialize(metronome);

      if (_chartData.SongMetaData.Audio.SongStream != null)
        _audioController.SetStream(_chartData.SongMetaData.Audio.SongStream);

      _groupController.Initialize(_chartData.Groups);
      _themeController.Initialize(_chartData.ThemeChannels);

      _noteController.Initialize(metronome, _chartData.Windows, Autoplay);
      _noteController.PlayerNoteSize = NoteSize;
      _noteController.PlayerNoteSpeed = NoteSpeed;

      _hitFXController.Initialize(_hitFXLayer, _noteController);
      foreach (var pack in ResourcePackManager.Instance.GetAllResourcePacks())
        _hitFXController.Prewarm(pack);

      _windowController.Initialize(
        _objectsLayer, _chartData.Windows, metronome,
        _groupController, _themeController, _noteController
      );
      _componentController.Initialize(
        _chartData.Components, metronome,
        _chartData.SongMetaData, _chartData.ChartMetadata
      );

      _hitController.Initialize(_audioController, _noteController, _windowController);

      _scoreTracker.SetTotalCombos(_chartData.Windows.TotalComboCount);

      _componentController.SetAccuracy(1f);
      _componentController.SetScore(0);
      _componentController.SetCombo(0);
      _componentController.SetStatus(Autoplay ? PlayerCombo.Status.AT : PlayerCombo.Status.AP);

      ApplyScreenSize();

      var parent = GetParent<Control>();
      if (parent != null)
        parent.Connect("item_rect_changed", this, nameof(OnParentResized));

      // Wait for Godot splash and renderer to settle before starting playback.
      await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

      _audioController.Resume();
    }

    // ── Setters ──────────────────────────────────────────────────────────────────

    public void SetAutoPlay(bool active) => Autoplay = active;
    public void SetNoteSize(float size) => NoteSize = size;
    public void SetNoteSpeed(float speed) => NoteSpeed = speed;
    public void SetNoteHighLightSimulation(bool active) => NoteHighLightSimulation = active;

    // ── Screen resize ────────────────────────────────────────────────────────────

    private void OnParentResized() => ApplyScreenSize();

    private void ApplyScreenSize()
    {
      if (_windowController == null) return;

      _windowController.ScreenSize = OS.WindowSize;

      var parent = GetParent<Control>();
      _windowController.PlayerAreaSize = parent != null ? parent.RectSize : RectSize;
    }
  }
}