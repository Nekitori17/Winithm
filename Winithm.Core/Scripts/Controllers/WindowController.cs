using Godot;
using System.Collections.Generic;
using Winithm.Core.Behaviors;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  [Tool]
  public class WindowController : Node
  {

    protected AudioController _audioController;
    protected GroupController _groupController;
    protected ThemeChannelController _themeController;
    protected NoteController _noteController;
    protected WindowManager _windowManager;

    private Control _playfield;
    private PackedScene _windowScene;

    [Export] public Vector2 ScreenSize = new Vector2(1280, 720);
    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);

    [Export] public Color TitleBarColor = Colors.Coral;
    [Export] public Color TitleTextColor = Colors.Black;

    [Export] public float FocusablePulseFrequency = 10f;

    private class WindowState
    {
      public Window Visual;
      public WindowData Data;
      public ulong FrameSessionToken = 0;
    }

    private Dictionary<string, WindowState> _windowStates = new Dictionary<string, WindowState>();

    private double _lastUpdateBeat = -1f;
    private int _renderCursor = 0;
    private ulong _frameSessionToken = 1;

    private NodePool<Window> _windowPool;

    public WindowController(
      Control playfield,
      WindowManager windowManager,
      AudioController audioController,
      GroupController groupController,
      ThemeChannelController themeController,
      NoteController noteController
    )
    {
      if (_windowScene == null)
        _windowScene = GD.Load<PackedScene>("res://Winithm.Core/Resources/Sprites/Window.tscn");

      _playfield = playfield;
      _windowPool = new NodePool<Window>(this, _windowScene);

      _audioController = audioController;
      _groupController = groupController;
      _themeController = themeController;
      _noteController = noteController;
      _windowManager = windowManager;

      _windowStates.Clear();
      _renderCursor = 0;
      _frameSessionToken = 0;
      _lastUpdateBeat = -1f;
    }

    public void Update(double currentBeat)
    {
      if (_lastUpdateBeat == currentBeat) return;

      ForceUpdate(currentBeat, false);

      _lastUpdateBeat = currentBeat;
    }

    public void ForceUpdate(double currentBeat, bool _force = true)
    {
      if (_audioController == null || _windowManager == null) return;

      bool isBackward = currentBeat < _lastUpdateBeat;
      var maxEnds = _windowManager.MaxEndBeats;
      int windowCount = _windowManager.Count;

      _frameSessionToken++;

      if (isBackward)
      {
        _renderCursor = FindRenderCursor(maxEnds, currentBeat);
      }
      else
      {
        while (_renderCursor < windowCount && maxEnds[_renderCursor] < currentBeat)
        {
          _renderCursor++;
        }
      }

      for (int i = _renderCursor; i < windowCount; i++)
      {
        var windowData = _windowManager[i];

        if (windowData.StartBeat.AbsoluteValue > currentBeat) break;

        float lifeCycleScale = CalculateLifeCycleScale(windowData, currentBeat);
        bool shouldBeActive = lifeCycleScale > 0.001f;

        bool isActive = _windowStates.TryGetValue(windowData.ID, out WindowState state);
        if (!shouldBeActive)
        {
          continue;
        }


        Window windowVisual;
        if (!isActive)
        {
          if (_windowScene == null) continue;

          windowVisual = _windowPool.Get();
          windowVisual.Name = string.IsNullOrEmpty(windowData.ID) ? "Window" : windowData.ID;
          windowVisual.Pivot = windowData.Anchor;
          windowVisual.Title = windowData.Title;
          windowVisual.Borderless = windowData.Borderless;
          windowVisual.TitleBarColor = TitleBarColor;
          windowVisual.TitleTextColor = TitleTextColor;

          state = new WindowState { Visual = windowVisual, Data = windowData };

          _windowStates[windowData.ID] = state;
          _noteController.RegisterWindow(windowData.ID, windowData, windowVisual);

          if (windowVisual.GetParent() != _playfield)
          {
            windowVisual.GetParent()?.RemoveChild(windowVisual);
            _playfield.AddChild(windowVisual);
          }

          windowVisual.ZIndex = LayerUtils.ComposeLayerIndex(windowData.Layer, windowData.SubLayer);
        }
        else
        {
          windowVisual = state.Visual;
        }

        state.FrameSessionToken = _frameSessionToken;

        float x = EvaluateProperty(
          windowData, StoryboardProperty.X, currentBeat, windowData.InitX, _force
        );
        float y = EvaluateProperty(
          windowData, StoryboardProperty.Y, currentBeat, windowData.InitY, _force
        );
        float scaleX = EvaluateProperty(
          windowData, StoryboardProperty.ScaleX, currentBeat, windowData.InitScaleX, _force
        );
        float scaleY = EvaluateProperty(
          windowData, StoryboardProperty.ScaleY, currentBeat, windowData.InitScaleY, _force
        );

        if (windowData.StoryboardEvents != null && windowData.StoryboardEvents.TryGetValue(StoryboardProperty.Title, out var titleEvents) && titleEvents.Count > 0)
        {
          var titleVal = windowData.StoryboardEvents.Evaluate(
            StoryboardProperty.Title, currentBeat, new AnyValue(windowData.Title), _force
          );
          if (titleVal.Type == AnyValueType.String) windowVisual.Title = titleVal.StringValue;
        }

        float animScale = Mathf.Lerp(0.95f, 1.0f, lifeCycleScale);

        Vector2 finalPos = new Vector2(x, y);
        Vector2 finalScale = new Vector2(scaleX, scaleY) * animScale;

        if (_groupController != null && !string.IsNullOrEmpty(windowData.GroupID))
        {
          var gNode = _force ?
            _groupController.ForceGetGroupNode(windowData.GroupID, currentBeat)
            : _groupController.GetGroupNode(windowData.GroupID, currentBeat);

          if (gNode != null)
          {
            Transform2D gTrans = gNode.GlobalTransform;
            finalPos = gTrans * finalPos;

            finalScale.x *= gNode.GlobalScale.x;
            finalScale.y *= gNode.GlobalScale.y;
          }
        }

        windowVisual.Position = finalPos;
        windowVisual.RotationDegrees = 0f;
        windowVisual.WindowSize = finalScale;

        Color finalWindowColor = windowVisual.WindowColor;
        float finalNoteA = windowVisual.NoteOpacity;

        if (_themeController != null && !string.IsNullOrEmpty(windowData.ThemeChannelID) && _themeController.HasThemeChannel(windowData.ThemeChannelID))
        {
          var themeColor = _themeController.GetThemeColor(windowData.ThemeChannelID, currentBeat);
          if (themeColor.HasValue)
          {
            finalWindowColor = themeColor.Value.WindowColor;
            finalNoteA = themeColor.Value.NoteA;
          }
        }
        else
        {
          float r = EvaluateProperty(
            windowData, StoryboardProperty.ColorR, currentBeat, windowData.InitR, _force
          );
          float g = EvaluateProperty(
            windowData, StoryboardProperty.ColorG, currentBeat, windowData.InitG, _force
          );
          float b = EvaluateProperty(
            windowData, StoryboardProperty.ColorB, currentBeat, windowData.InitB, _force
          );
          float a = EvaluateProperty(
            windowData, StoryboardProperty.ColorA, currentBeat, windowData.InitA, _force
          );
          float noteA = EvaluateProperty(
            windowData, StoryboardProperty.NoteA, currentBeat, windowData.InitNoteA, _force
          );

          finalWindowColor = new Color(r, g, b, a);
          finalNoteA = noteA;
        }

        windowVisual.WindowColor = finalWindowColor;
        windowVisual.NoteOpacity = finalNoteA;
        windowVisual.Modulate = new Color(1, 1, 1, lifeCycleScale);

        windowVisual.ScreenSize = ScreenSize;
        windowVisual.PlayerAreaSize = PlayerAreaSize;

        if (windowData.UnFocus)
          AnimateFocusableOverlay(windowVisual, windowData, currentBeat);
        else
        {
          windowVisual.UnFocusOverlayOpacity = 0f;
          windowVisual.UnFocus = false;
        }

        if (windowData.Unresponsive)
          AnimateUnresponsiveOverlay(windowVisual, windowData, currentBeat);
        else
        {
          windowVisual.UnresponsiveOverlayOpacity = 0f;
          windowVisual.IsNotRespondingTitle = false;
        }

        windowVisual.UpdateVisual();
      }

      CollectStaleWindows();
    }

    private void CollectStaleWindows()
    {
      var staleIds = new List<string>();
      foreach (var kvp in _windowStates)
      {
        if (kvp.Value.FrameSessionToken != _frameSessionToken)
        {
          staleIds.Add(kvp.Key);
        }
      }

      foreach (var id in staleIds)
      {
        _windowPool.Release(_windowStates[id].Visual);
        _windowStates.Remove(id);
        _noteController.UnregisterWindow(id);
      }
    }

    private int FindRenderCursor(double[] maxEnds, double currentBeat)
    {
      if (maxEnds == null || maxEnds.Length == 0) return 0;
      int left = 0, right = maxEnds.Length - 1;
      int best = maxEnds.Length;

      while (left <= right)
      {
        int mid = left + (right - left) / 2;
        if (maxEnds[mid] >= currentBeat)
        {
          best = mid;
          right = mid - 1;
        }
        else
        {
          left = mid + 1;
        }
      }
      return best;
    }

    private void AnimateFocusableOverlay(
      Window windowVisual, WindowData windowData, double currentBeat
    )
    {
      if (currentBeat < windowData.FocusableStartBeat)
      {
        windowVisual.UnFocusOverlayOpacity = Window.UNFOCUS_OVERLAY_TINT;
        windowVisual.UnFocus = true;
        return;
      }
      else if (currentBeat < windowData.FocusableEndBeat)
      { // Focus pulse: deterministic sin wave based on beat for perfect scrub rendering
        float sinVal = Mathf.Sin((float)currentBeat * FocusablePulseFrequency * Mathf.Pi);
        windowVisual.UnFocusOverlayOpacity = sinVal > 0 ? Window.UNFOCUS_OVERLAY_TINT : 0f;
        windowVisual.UnFocus = true;
      }
      else
      {
        windowVisual.UnFocusOverlayOpacity = 0f;
        windowVisual.UnFocus = false;
      }
    }

    private void AnimateUnresponsiveOverlay(
      Window windowVisual, WindowData windowData, double currentBeat
    )
    {
      if (currentBeat < windowData.UnresponsiveStartBeat)
      {
        windowVisual.UnresponsiveOverlayOpacity = 0f;
        windowVisual.IsNotRespondingTitle = false;
      }
      else if (currentBeat < windowData.UnresponsiveEndBeat)
      {
        windowVisual.IsNotRespondingTitle = true;

        double t =
          (currentBeat - windowData.UnresponsiveStartBeat)
          / (windowData.UnresponsiveEndBeat - windowData.UnresponsiveStartBeat);

        windowVisual.UnresponsiveOverlayOpacity =
          (float)EasingFunctions.Evaluate(EasingType.CubicOut, t);
      }
      else
      {
        windowVisual.IsNotRespondingTitle = true;
        windowVisual.UnresponsiveOverlayOpacity = 1f;
      }
    }

    /// <summary>
    /// Called at runtime when a window enters the UnResponsive state.
    /// Computes overlay animation timestamps and extends the window's lifetime.
    /// </summary>
    public void SetUnresponsive(string windowId)
    {
      if (!_windowStates.TryGetValue(windowId, out var state)) return;
      var windowData = state.Data;
      if (windowData.Unresponsive) return;

      windowData.Unresponsive = true;

      if (_audioController != null)
      {
        windowData.ComputeAnimationWhenUnresponsive(_audioController.Metronome);
      }
    }

    public void SetStartFocusable(string windowId, float currentBeat)
    {
      if (!_windowStates.TryGetValue(windowId, out var state)) return;
      var windowData = state.Data;
      if (windowData.UnFocus) return;

      windowData.UnFocus = true;
      windowData.FocusableStartBeat = currentBeat;
    }

    public void SetEndFocusable(string windowId, float currentBeat)
    {
      if (!_windowStates.TryGetValue(windowId, out var state)) return;
      var windowData = state.Data;
      if (!windowData.UnFocus) return;

      windowData.FocusableEndBeat = currentBeat;
    }

    /// <summary>
    /// Lifecycle scale for spawn/despawn animations.
    /// Purely beat-driven interpolation using accurate pre-computed animation bounds.
    /// </summary>
    protected float CalculateLifeCycleScale(WindowData windowData, double currentBeat)
    {
      if (currentBeat < windowData.StartInStartBeat) return 0f;
      if (currentBeat > windowData.EndOutEndBeat) return 0f;

      // Spawn fade-in
      if (currentBeat < windowData.StartInEndBeat)
      {
        double t = (currentBeat - windowData.StartInStartBeat) / (windowData.StartInEndBeat - windowData.StartInStartBeat);
        return (float)EasingFunctions.Evaluate(EasingType.CubicOut, t);
      }

      // Despawn fade-out
      if (currentBeat >= windowData.EndOutStartBeat)
      {
        double t = (currentBeat - windowData.EndOutStartBeat) / (windowData.EndOutEndBeat - windowData.EndOutStartBeat);
        return (float)(1f - EasingFunctions.Evaluate(EasingType.CubicIn, t));
      }

      return 1f;
    }

    protected float EvaluateProperty(
      WindowData windowData,
      StoryboardProperty propType,
      double currentBeat,
      float defaultValue,
      bool force
    )
    {
      if (windowData.StoryboardEvents == null || !windowData.StoryboardEvents.TryGetValue(propType, out _)) return defaultValue;

      return windowData.StoryboardEvents.Evaluate(
        propType, currentBeat, new AnyValue(defaultValue), force
      ).X;
    }
  }
}