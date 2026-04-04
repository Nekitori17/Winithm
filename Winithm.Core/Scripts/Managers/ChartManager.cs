using Godot;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Logic;
using Winithm.Core.Common;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// The top-level chart manager that orchestrates all sub-managers.
  /// Owns the TimeManager, StoryboardEvaluator, WindowManager, GroupManager, ThemeChannelManager, etc.
  /// Drives the global playback state including the "Rewind On Resume" logic.
  /// </summary>
  [Tool]
  public class ChartManager : Node
  {
    [Export] public PackedScene WindowScene { get; set; }

    // Sub-managers (created or injected externally)
    public TimeManager TimeManager { get; private set; }
    public GroupManager GroupManager { get; private set; }
    public ThemeChannelManager ThemeChannelManager { get; private set; }
    public WindowManager WindowManager { get; private set; }

    // Rewind On Resume state
    private bool _isRewinding = false;
    private float _rewindStartBeat;
    private float _rewindTargetBeat;
    private float _rewindElapsed;
    private const float RewindDurationSec = 0.5f;  // 500ms animation
    private const float RewindAmountSec = 3.0f;     // Rewind 3 seconds

    // Pause state
    private bool _isPaused = false;
    private float _pauseBeat;

    public override void _Ready()
    {
      // Discover or create sub-managers
      TimeManager = GetNodeOrNull<TimeManager>("TimeManager") ?? new TimeManager();
      GroupManager = GetNodeOrNull<GroupManager>("GroupManager") ?? new GroupManager();
      ThemeChannelManager = GetNodeOrNull<ThemeChannelManager>("ThemeChannelManager") ?? new ThemeChannelManager();

      // WindowManager
      WindowManager = GetNodeOrNull<WindowManager>("WindowManager");
      if (WindowManager == null)
      {
        WindowManager = new WindowManager
        {
          Name = "WindowManager"
        };
        AddChild(WindowManager);
      }

      if (WindowScene != null) WindowManager.WindowScene = WindowScene;
      WindowManager.InjectManagers(TimeManager, GroupManager, ThemeChannelManager);
    }

    /// <summary>
    /// Loads all chart data into the appropriate sub-managers.
    /// </summary>
    public void LoadChart(List<WindowData> windows /* , List<GroupData> groups, etc. */)
    {
      WindowManager.LoadWindows(windows);
      // Future: GroupManager.LoadGroups(groups), ThemeChannelManager.LoadChannels(...), etc.
    }

    /// <summary>
    /// Pause the chart. Calling Resume() after this will trigger Rewind On Resume.
    /// </summary>
    public void Pause()
    {
      if (_isPaused) return;
      _isPaused = true;
      _pauseBeat = TimeManager.CurrentBeat;
      TimeManager.Pause();
    }

    /// <summary>
    /// Resume from pause with a 3-second rewind scrub over 500ms (cubic easing).
    /// </summary>
    public void Resume()
    {
      if (!_isPaused) return;
      _isPaused = false;

      float bps = TimeManager.GetCurrentBPS();
      float rewindBeats = RewindAmountSec * bps;

      _rewindStartBeat = _pauseBeat;
      _rewindTargetBeat = Mathf.Max(0f, _pauseBeat - rewindBeats);
      _rewindElapsed = 0f;
      _isRewinding = true;

      // During rewind, TimeManager is manually scrubbed
    }

    public override void _Process(float delta)
    {
      if (_isRewinding)
      {
        _rewindElapsed += delta;
        float t = Mathf.Clamp(_rewindElapsed / RewindDurationSec, 0f, 1f);
        // Cubic ease-out for smooth deceleration
        float eased = EasingFunctions.Evaluate(EasingType.CubicOut, t);
        float currentBeat = Mathf.Lerp(_rewindStartBeat, _rewindTargetBeat, eased);

        TimeManager.Seek(currentBeat);

        if (t >= 1f)
        {
          _isRewinding = false;
          TimeManager.Resume();
        }
      }
    }

    /// <summary>
    /// Sets the viewport and gameplay area sizes on the WindowManager.
    /// </summary>
    public void SetSizes(Vector2 viewportSize, Vector2 gameplayAreaSize)
    {
      WindowManager.ViewportSize = viewportSize;
      WindowManager.GameplayAreaSize = gameplayAreaSize;
    }
  }
}
