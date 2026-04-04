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
    [Export] public PackedScene WindowScene;
    
    protected TimeManager _timeManager;
    protected GroupManager _groupManager;
    protected ThemeChannelManager _themeManager;
    
    [Export] public Vector2 ScreenSize = new Vector2(1280, 720);
    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);

    protected List<WindowData> _windowDataList = new List<WindowData>();
    protected Dictionary<string, Window> _activeWindows = new Dictionary<string, Window>();

    protected Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>> _cursors 
      = new Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>>();

    protected ObjectPool<Window> _windowPool;

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
        maxSize: 1000
      );
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
        // Pre-compute ms timestamps from Metronome
        if (_timeManager != null)
        {
          wd.PreComputeAnimation(_timeManager.Metronome);
        }

        var propCursors = new Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>();
        if (wd.StoryboardEvents != null)
        {
          foreach (var prop in wd.StoryboardEvents.Keys)
            propCursors[prop] = new StoryboardEvaluator.Cursor();
        }
        _cursors[wd.ID] = propCursors;
      }
    }

    public void Update(float currentBeat)
    {
      if (_timeManager == null || _windowDataList == null) return;
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
        float noteA = EvaluateProperty(wd, StoryboardProperty.NoteA, currentBeat, wd.InitNoteA, cursors);
        w.NoteOpacity = noteA;
        
        if (wd.StoryboardEvents != null && wd.StoryboardEvents.TryGetValue(StoryboardProperty.Title, out var titleEvents) && titleEvents.Count > 0)
        {
          var titleVal = StoryboardEvaluator.Evaluate(titleEvents, currentBeat, new AnyValue(wd.Title), GetCursor(cursors, StoryboardProperty.Title));
          if (titleVal.Type == AnyValueType.String) w.Title = titleVal.StringValue;
        }

        float animScale = Mathf.Lerp(0.95f, 1.0f, lifeCycleScale);
        w.RectPosition = new Vector2(x, y);
        w.WindowSize = new Vector2(scaleX, scaleY) * animScale;

        Color finalWindowColor = w.WindowColor;
        float finalNoteA = w.NoteOpacity;
        if (_themeManager != null && !string.IsNullOrEmpty(wd.ThemeChannelID))
        { 
          var themeColor = _themeManager.GetThemeColor(wd.ThemeChannelID, currentBeat, w.WindowColor);
          finalWindowColor = themeColor.WindowColor;
          finalNoteA = themeColor.NoteA;
        }
        finalWindowColor.a = colorA;
        w.WindowColor = finalWindowColor;
        
        w.Modulate = new Color(1, 1, 1, lifeCycleScale);

        w.ScreenSize = ScreenSize;
        w.PlayerAreaSize = PlayerAreaSize;
        ApplyFlags(w, wd, currentBeat);
        w.UpdateVisual();
      }
    }

    /// <summary>
    /// Called at runtime when a window enters the UnResponsive state.
    /// Computes overlay animation timestamps and extends the window's lifetime.
    /// </summary>
    public void SetUnresponsive(string windowId)
    {
      var wd = _windowDataList.Find(w => w.ID == windowId);
      if (wd == null || wd.Unresponsive) return;
      
      if (_timeManager != null)
      {
        wd.ComputeWhenUnresponsiveAnimation(_timeManager.Metronome);
      }
    }

    protected virtual void ApplyFlags(Window w, WindowData wd, float currentBeat)
    {
      w.UnFocus = wd.UnFocus;

      // Focus pulse: deterministic sin wave based on beat for perfect scrub rendering
      if (w.UnFocus && currentBeat >= wd.FocusableStartBeat && currentBeat <= wd.FocusableEndBeat)
      {
        float pulseFrequency = 10f;
        float sinVal = Mathf.Sin(currentBeat * pulseFrequency * Mathf.Pi);
        w.FocusOverlayOpacity = sinVal > 0 ? 0.1f : 0f;
      }
      else
      {
        w.FocusOverlayOpacity = 0f;
      }

      // UnResponsive overlay: CubicOut fade from StartBeat -> EndBeatUnresponsive
      if (wd.Unresponsive)
      {
        if (currentBeat >= wd.EndBeat.AbsoluteValue && currentBeat <= wd.EndBeatUnresponsive)
        {
          float t = (currentBeat - wd.EndBeat.AbsoluteValue) / (wd.EndBeatUnresponsive - wd.EndBeat.AbsoluteValue);
          w.UnresponsiveOverlayOpacity = EasingFunctions.Evaluate(EasingType.CubicOut, t);
        }
        else if (currentBeat > wd.EndBeatUnresponsive)
        {
          w.UnresponsiveOverlayOpacity = 1f;
        }
        else
        {
          w.UnresponsiveOverlayOpacity = 0f;
        }

        // Title jumps to "Not Responding" the instant we hit EndBeat
        w.IsNotRespondingTitle = currentBeat >= wd.EndBeat.AbsoluteValue;
      }
      else
      {
        w.UnresponsiveOverlayOpacity = 0f;
        w.IsNotRespondingTitle = false;
      }
    }

    /// <summary>
    /// Lifecycle scale for spawn/despawn animations.
    /// Purely beat-driven interpolation using accurate pre-computed animation bounds.
    /// </summary>
    protected float CalculateLifeCycleScale(WindowData wd, float currentBeat, float bps)
    {
      if (currentBeat < wd.StartBeat.AbsoluteValue) return 0f;
      if (currentBeat > wd.EndBeatEndOut) return 0f;

      // Spawn fade-in
      if (currentBeat < wd.EndBeatStartIn)
      {
        float t = (currentBeat - wd.StartBeat.AbsoluteValue) / (wd.EndBeatStartIn - wd.StartBeat.AbsoluteValue);
        return EasingFunctions.Evaluate(EasingType.CubicOut, t);
      }

      // Despawn fade-out
      if (currentBeat >= wd.StartBeatEndOut)
      {
        float t = (currentBeat - wd.StartBeatEndOut) / (wd.EndBeatEndOut - wd.StartBeatEndOut);
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