using Godot;
using Winithm.Core;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class ChartInfo : Control
  {
    public struct LastState
    {
      public string DifficultText;
      public Color TextColor, TextOutLineColor;
    }

    [Export] public Vector2 ScreenSize = Constants.Visual.DESIGN_RESOLUTION;
    [Export] public string DifficultText = "Info: 5";
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;

    private LastState _lastState = new LastState();

    private Label _difficult;

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
      bool isColorDirty = 
        TextColor != _lastState.TextColor 
        || TextOutLineColor != _lastState.TextOutLineColor;
      bool isInfoDirty = DifficultText != _lastState.DifficultText;

      if (isColorDirty) UpdateColor();
      if (isInfoDirty) UpdateInfo();
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
