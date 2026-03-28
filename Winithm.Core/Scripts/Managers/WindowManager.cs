using Godot;
using System.Collections.Generic;
using Winithm.Core.Behaviors;
using Winithm.Core.Data;
using Winithm.Core.Logic;
using Winithm.Core.Common;

namespace Winithm.Core.Managers
{
  [Tool]
  public class WindowManager : Node
  {
    [Export] public PackedScene WindowScene { get; set; }
    
    protected TimeManager _timeManager;
    protected GroupManager _groupManager;
    protected ThemeChannelManager _themeManager;
    protected ObjectPool<Window> _windowPool;
    
    private Vector2 _viewportSize = new Vector2(1280, 720);
    public Vector2 ViewportSize
    {
      get => _viewportSize;
      set { _viewportSize = value; }
    }

    private Vector2 _gameplayAreaSize = new Vector2(1280, 720);
    public Vector2 GameplayAreaSize
    {
      get => _gameplayAreaSize;
      set { _gameplayAreaSize = value; }
    }

    protected List<WindowData> _windowDataList = new List<WindowData>();
    protected Dictionary<string, Window> _activeWindows = new Dictionary<string, Window>();

    protected Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>> _cursors 
      = new Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>>();

    public override void _Ready()
    {
      if (WindowScene == null)
        WindowScene = GD.Load<PackedScene>("res://Winithm.Core/Resources/Sprites/Window.tscn");
      
      _windowPool = new ObjectPool<Window>(
        createFunc: () => WindowScene.Instance<Window>(),
        actionOnGet: w => w.Visible = true,
        actionOnRelease: w => 
        { 
          w.Visible = false;
          if (w.GetParent() != null) w.GetParent().RemoveChild(w);
        },
        actionOnDestroy: w => w.QueueFree(),
        collectionCheck: false,
        defaultCapacity: 10,
        maxSize: 100
      );

      SetProcess(true);
    }

    public void InjectManagers(TimeManager timeManager, GroupManager groupManager, ThemeChannelManager themeManager)
    {
      _timeManager = timeManager;
      _groupManager = groupManager;
      _themeManager = themeManager;
    }

    public void LoadWindows(List<WindowData> windows)
    {
      foreach (var window in _activeWindows.Values)
      {
        _windowPool.Release(window);
      }
      _activeWindows.Clear();
      _cursors.Clear();

      _windowDataList = windows ?? new List<WindowData>();

      foreach (var wd in _windowDataList)
      {
        var propCursors = new Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>();
        if (wd.StoryboardEvents != null)
        {
          foreach (var prop in wd.StoryboardEvents.Keys)
            propCursors[prop] = new StoryboardEvaluator.Cursor();
        }
        _cursors[wd.ID] = propCursors;
      }
    }

    public override void _Process(float delta)
    {
      if (_timeManager == null || _windowDataList == null) return;
      float currentBeat = _timeManager.CurrentBeat;
      float currentBPS = _timeManager.GetCurrentBPS();

      foreach (var wd in _windowDataList)
      {
        float lifeCycleScale = CalculateLifeCycleScale(wd, currentBeat, currentBPS);
        bool shouldBeActive = lifeCycleScale > 0.001f;
        bool isActive = _activeWindows.TryGetValue(wd.ID, out Window w);

        if (!shouldBeActive)
        {
          if (isActive)
          {
            _windowPool.Release(w);
            _activeWindows.Remove(wd.ID);
          }
          continue;
        }

        if (!isActive)
        {
          if (WindowScene == null) continue;
          
          w = _windowPool.Get();
          w.Name = string.IsNullOrEmpty(wd.ID) ? "Window" : wd.ID;
          w.Title = wd.Title;
          w.Borderless = wd.Borderless;
          
          ApplyFlags(w, wd, currentBeat);

          _activeWindows[wd.ID] = w;

          Node parentNode = this;
          if (_groupManager != null && !string.IsNullOrEmpty(wd.GroupID))
          {
             var gNode = _groupManager.GetAndUpdateGroupNode(wd.GroupID, currentBeat);
             if (gNode != null) parentNode = gNode;
          }
          parentNode.AddChild(w);
        }

        var cursors = _cursors[wd.ID];

        float x = EvaluateProperty(wd, StoryboardProperty.X, currentBeat, wd.InitX, cursors);
        float y = EvaluateProperty(wd, StoryboardProperty.Y, currentBeat, wd.InitY, cursors);
        float scaleX = EvaluateProperty(wd, StoryboardProperty.ScaleX, currentBeat, wd.InitScaleX, cursors);
        float scaleY = EvaluateProperty(wd, StoryboardProperty.ScaleY, currentBeat, wd.InitScaleY, cursors);
        float colorA = EvaluateProperty(wd, StoryboardProperty.ColorA, currentBeat, wd.InitA, cursors);
        
        if (wd.StoryboardEvents != null && wd.StoryboardEvents.TryGetValue(StoryboardProperty.Title, out var titleEvents) && titleEvents.Count > 0)
        {
          var titleVal = StoryboardEvaluator.Evaluate(titleEvents, currentBeat, new AnyValue(wd.Title), GetCursor(cursors, StoryboardProperty.Title));
          if (titleVal.Type == AnyValueType.String) w.Title = titleVal.StringValue;
        }

        float animScale = Mathf.Lerp(0.95f, 1.0f, lifeCycleScale);
        w.RectPosition = new Vector2(x, y);
        w.WindowSize = new Vector2(scaleX, scaleY) * animScale;

        Color finalWindowColor = w.WindowColor;
        if (_themeManager != null && !string.IsNullOrEmpty(wd.ThemeChannelID))
        {
          finalWindowColor = _themeManager.GetThemeColor(wd.ThemeChannelID, currentBeat, w.WindowColor);
        }
        finalWindowColor.a = colorA;
        w.WindowColor = finalWindowColor;
        
        w.Modulate = new Color(1, 1, 1, lifeCycleScale);

        w.ViewportSize = _viewportSize;
        w.GameplayAreaSize = _gameplayAreaSize;
        ApplyFlags(w, wd, currentBeat);
        w.UpdateWindow();
      }
    }

    protected virtual void ApplyFlags(Window w, WindowData wd, float currentBeat)
    {
      // Basic implementation: follow the data (may be overridden by input logic in subclasses)
      w.Unfocus = wd.UnFocus;

      // State-driven focus flash: a deterministic sin wave based on beat, not delta time.
      // This allows perfect rewind/scrub rendering.
      if (w.Unfocus && w.Focusable)
      {
        float pulseFrequency = 10f; // Oscillations per beat
        float sinVal = Mathf.Sin(currentBeat * pulseFrequency * Mathf.Pi);
        w.FocusOverlayOpacity = sinVal > 0 ? 0.1f : 0f;
      }
      else
      {
        w.FocusOverlayOpacity = 0f;
      }

      // Default: no unresponsive overlay
      w.UnresponsiveOverlayOpacity = 0f;
      w.IsNotRespondingTitle = false;
    }

    protected float CalculateLifeCycleScale(WindowData wd, float currentBeat, float bps)
    {
      float animDuration = 0.2f * bps; // 200ms translated to beats based on current BPM.

      if (currentBeat < wd.StartBeat) return 0f;
      if (currentBeat > wd.EndBeat + animDuration) return 0f;

      if (currentBeat >= wd.StartBeat && currentBeat < wd.StartBeat + animDuration)
      {
        float t = (currentBeat - wd.StartBeat) / animDuration;
        return EasingFunctions.Evaluate(EasingType.CubicOut, t);
      }
      
      if (currentBeat >= wd.EndBeat && currentBeat <= wd.EndBeat + animDuration)
      {
        float t = (currentBeat - wd.EndBeat) / animDuration;
        return 1f - EasingFunctions.Evaluate(EasingType.CubicIn, t);
      }

      return 1f;
    }

    protected StoryboardEvaluator.Cursor GetCursor(Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor> cursors, StoryboardProperty prop)
    {
      if (cursors.TryGetValue(prop, out var cursor)) return cursor;
      return null;
    }

    protected float EvaluateProperty(WindowData wd, StoryboardProperty propType, float beat, float defaultValue, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor> cursors)
    {
      if (wd.StoryboardEvents == null || !wd.StoryboardEvents.TryGetValue(propType, out var events)) return defaultValue;
      return StoryboardEvaluator.Evaluate(events, beat, new AnyValue(defaultValue), GetCursor(cursors, propType)).X;
    }
  }
}