using Godot;
using System.Collections.Generic;
using Winithm.Core.Behaviors;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Logic;

namespace Winithm.Core.Managers
{
  [Tool]
  public class WindowManager : Node
  {

    protected TimeManager _timeManager;
    protected GroupManager _groupManager;
    protected ThemeChannelManager _themeManager;
    protected NoteManager _noteManager;

    private PackedScene _windowScene;

    [Export] public Vector2 ScreenSize = new Vector2(1280, 720);
    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);

    [Export] public Color TitleBarColor = Colors.Coral;
    [Export] public Color TitleTextColor = Colors.Black;

    [Export] public float FocusablePulseFrequency = 10f;

    private List<WindowData> _windowDataList = new List<WindowData>();
    private Dictionary<string, Window> _activeWindows = new Dictionary<string, Window>();

    private Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>> _cursors
      = new Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>>();

    private float _lastUpdateBeat = -1f;

    private ObjectPool<Window> _windowPool;

    public override void _Ready()
    {
      if (_windowScene == null)
        _windowScene = GD.Load<PackedScene>("res://Winithm.Core/Resources/Sprites/Window.tscn");

      _windowPool = new NodePool<Window>(this, _windowScene);
    }

    public void InjectManagers(
      TimeManager timeManager,
      GroupManager groupManager,
      ThemeChannelManager themeManager,
      NoteManager noteManager
    )
    {
      _timeManager = timeManager;
      _groupManager = groupManager;
      _themeManager = themeManager;
      _noteManager = noteManager;
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

    public void Update()
    {
      if (_timeManager == null || _windowDataList == null) return;
      float currentBeat = _timeManager.CurrentBeat;
      if (_lastUpdateBeat == currentBeat) return;

      ForceUpdate(currentBeat, false);

      _lastUpdateBeat = currentBeat;
    }

    public void ForceUpdate(float currentBeat, bool _force = true)
    {
      if (_timeManager == null || _windowDataList == null) return;
      float currentBPS = _timeManager.GetCurrentBPS();

      foreach (var wd in _windowDataList)
      {
        float lifeCycleScale = CalculateLifeCycleScale(wd, currentBeat, currentBPS);
        bool shouldBeActive = lifeCycleScale > 0.001f;

        bool isActive = _activeWindows.TryGetValue(wd.ID, out Window w);

        if (!shouldBeActive && isActive)
        {
          _windowPool.Release(w);
          _activeWindows.Remove(wd.ID);
          _noteManager.UnregisterWindow(wd.ID);

          continue;
        }

        if (!isActive)
        {
          if (_windowScene == null) continue;

          w = _windowPool.Get();
          w.Name = string.IsNullOrEmpty(wd.ID) ? "Window" : wd.ID;
          w.Pivot = wd.Anchor;
          w.Title = wd.Title;
          w.Borderless = wd.Borderless;
          w.TitleBarColor = TitleBarColor;
          w.TitleTextColor = TitleTextColor;

          _activeWindows[wd.ID] = w;
          _noteManager.RegisterWindow(wd.ID, wd, w);

          if (w.GetParent() != this)
          {
            w.GetParent()?.RemoveChild(w);
            AddChild(w);
          }

          MoveChild(w, wd.Layer);
        }

        var cursors = _cursors[wd.ID];

        float x = EvaluateProperty(wd, StoryboardProperty.X, currentBeat, wd.InitX, cursors);
        float y = EvaluateProperty(wd, StoryboardProperty.Y, currentBeat, wd.InitY, cursors);
        float scaleX = EvaluateProperty(wd, StoryboardProperty.ScaleX, currentBeat, wd.InitScaleX, cursors);
        float scaleY = EvaluateProperty(wd, StoryboardProperty.ScaleY, currentBeat, wd.InitScaleY, cursors);

        if (wd.StoryboardEvents != null && wd.StoryboardEvents.TryGetValue(StoryboardProperty.Title, out var titleEvents) && titleEvents.Count > 0)
        {
          var titleVal = StoryboardEvaluator.Evaluate(titleEvents, currentBeat, new AnyValue(wd.Title), GetCursor(cursors, StoryboardProperty.Title));
          if (titleVal.Type == AnyValueType.String) w.Title = titleVal.StringValue;
        }

        float animScale = Mathf.Lerp(0.95f, 1.0f, lifeCycleScale);

        Vector2 finalPos = new Vector2(x, y);
        Vector2 finalScale = new Vector2(scaleX, scaleY) * animScale;
        if (_groupManager != null && !string.IsNullOrEmpty(wd.GroupID))
        {
          var gNode = _force ?
            _groupManager.ForceGetGroupNode(wd.GroupID, currentBeat)
            : _groupManager.GetGroupNode(wd.GroupID, currentBeat);

          if (gNode != null)
          {
            // Calculate actual physical position based on Group's rotation, scale, and position (Orbiting)
            Transform2D gTrans = gNode.GlobalTransform;
            finalPos = gTrans * finalPos;

            // Scale visually expands/shrinks the window dimensions (WindowBody)
            finalScale.x *= gNode.GlobalScale.x;
            finalScale.y *= gNode.GlobalScale.y;
          }
        }

        w.RectPosition = finalPos;
        w.RectRotation = 0f; // Window remains fully upright, bypassing structural tilt
        w.WindowSize = finalScale;

        Color finalWindowColor = w.WindowColor;
        float finalNoteA = w.NoteOpacity;

        if (_themeManager != null && !string.IsNullOrEmpty(wd.ThemeChannelID) && _themeManager.HasThemeChannel(wd.ThemeChannelID))
        {
          var themeColor = _themeManager.GetThemeColor(wd.ThemeChannelID, currentBeat, w.WindowColor);
          finalWindowColor = themeColor.WindowColor;
          finalNoteA = themeColor.NoteA;
        }
        else
        {
          float r = EvaluateProperty(wd, StoryboardProperty.ColorR, currentBeat, wd.InitR, cursors);
          float b = EvaluateProperty(wd, StoryboardProperty.ColorB, currentBeat, wd.InitB, cursors);
          float g = EvaluateProperty(wd, StoryboardProperty.ColorG, currentBeat, wd.InitG, cursors);
          float a = EvaluateProperty(wd, StoryboardProperty.ColorA, currentBeat, wd.InitA, cursors);
          float noteA = EvaluateProperty(wd, StoryboardProperty.NoteA, currentBeat, wd.InitNoteA, cursors);

          finalWindowColor = new Color(r, g, b, a);
          finalNoteA = noteA;
        }

        w.WindowColor = finalWindowColor;
        w.NoteOpacity = finalNoteA;
        w.Modulate = new Color(1, 1, 1, lifeCycleScale);

        w.ScreenSize = ScreenSize;
        w.PlayerAreaSize = PlayerAreaSize;


        if (wd.UnFocus)
          AnimateFocusableOverlay(w, wd, currentBeat);
        else
        {
          w.UnFocusOverlayOpacity = 0f;
          w.UnFocus = false;
        }

        if (wd.Unresponsive)
          AnimateUnresponsiveOverlay(w, wd, currentBeat);
        else
        {
          w.UnresponsiveOverlayOpacity = 0f;
          w.IsNotRespondingTitle = false;
        }

        w.UpdateVisual();
      }
    }

    private void AnimateFocusableOverlay(Window w, WindowData wd, float currentBeat)
    {
      if (currentBeat < wd.FocusableStartBeat)
      {
        w.UnFocusOverlayOpacity = Window.UNFOCUS_OVERLAY_TINT;
        w.UnFocus = true;
        return;
      }

      // Focus pulse: deterministic sin wave based on beat for perfect scrub rendering
      if (currentBeat >= wd.FocusableStartBeat && currentBeat <= wd.FocusableEndBeat)
      {
        float sinVal = Mathf.Sin(currentBeat * FocusablePulseFrequency * Mathf.Pi);
        w.UnFocusOverlayOpacity = sinVal > 0 ? Window.UNFOCUS_OVERLAY_TINT : 0f;
        w.UnFocus = true;
      }
      

      if (currentBeat > wd.FocusableEndBeat)
      {
        w.UnFocusOverlayOpacity = 0f;
        w.UnFocus = false;
      }
    }

    private void AnimateUnresponsiveOverlay(Window w, WindowData wd, float currentBeat)
    {
      if (currentBeat < wd.EndBeat.AbsoluteValue)
      {
        w.UnresponsiveOverlayOpacity = 0f;
        w.IsNotRespondingTitle = false;
        return;
      }

      w.IsNotRespondingTitle = true;

      if (currentBeat >= wd.EndBeatUnresponsive)
      {
        w.UnresponsiveOverlayOpacity = 1f;
        return;
      }

      if (currentBeat >= wd.EndBeat.AbsoluteValue && currentBeat <= wd.EndBeatUnresponsive)
      {
        float t =
          (currentBeat - wd.EndBeat.AbsoluteValue)
          / (wd.EndBeatUnresponsive - wd.EndBeat.AbsoluteValue);
        w.UnresponsiveOverlayOpacity = EasingFunctions.Evaluate(EasingType.CubicOut, t);
        return;
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

      wd.Unresponsive = true;

      if (_timeManager != null)
      {
        wd.ComputeAnimationWhenUnresponsive(_timeManager.Metronome);
      }
    }

    public void SetStartFocusable(string windowId, float currentBeat)
    {
      var wd = _windowDataList.Find(w => w.ID == windowId);
      if (wd == null || wd.UnFocus) return;

      wd.UnFocus = true;
      wd.FocusableStartBeat = currentBeat;
    }

    public void SetEndFocusable(string windowId, float currentBeat)
    {
      var wd = _windowDataList.Find(w => w.ID == windowId);
      if (wd == null || !wd.UnFocus) return;

      wd.FocusableEndBeat = currentBeat;
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