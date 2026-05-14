using Godot;
using Winithm.Core.Constants;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class ChartInfo : Control
  {
    public struct LastState
    {
      public Vector2 ScreenSize;
      public string DifficultText;
      public Color TextColor, TextOutLineColor;
    }

    [Export] public Vector2 ScreenSize = Visual.DESIGN_RESOLUTION;
    [Export] public string DifficultText = "Info: 5";
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;

    private LastState _lastState = new LastState();

    private Label _difficult;
    private readonly Vector2 BASE_DIFFICULT_POS = new Vector2(-25f, -90.5f);

    public override void _Ready()
    {
      _difficult = GetNode<Label>("Difficult");
      UpdateVisual();
    }

    public override void _Process(float delta)
    {
      UpdateVisual();
    }

    public void UpdateVisual()
    {
      bool isLayoutDirty = ScreenSize != _lastState.ScreenSize;
      bool isColorDirty = 
        TextColor != _lastState.TextColor 
        || TextOutLineColor != _lastState.TextOutLineColor;
      bool isInfoDirty = DifficultText != _lastState.DifficultText;

      if (isLayoutDirty) UpdateLayout();
      if (isColorDirty) UpdateColor();
      if (isInfoDirty) UpdateInfo();
    }

    private void UpdateLayout()
    {
      if (_difficult == null) return;

      float viewScale = Mathf.Abs(Mathf.Min(
        ScreenSize.x / Visual.DESIGN_RESOLUTION.x,
        ScreenSize.y / Visual.DESIGN_RESOLUTION.y
      ));

      _difficult.RectPosition = BASE_DIFFICULT_POS * viewScale;

      _lastState.ScreenSize = ScreenSize;
    }

    private void UpdateColor()
    {
      if (_difficult != null)
      {
        _difficult.AddColorOverride("font_color", TextColor);
        _difficult.AddColorOverride("font_outline_modulate", TextOutLineColor);
      }

      _lastState.TextColor = TextColor;
      _lastState.TextOutLineColor = TextOutLineColor;
    }

    private void UpdateInfo()
    {
      if (_difficult != null)
      {
        _difficult.Text = DifficultText;
      }

      _lastState.DifficultText = DifficultText;
    }
  }
}
