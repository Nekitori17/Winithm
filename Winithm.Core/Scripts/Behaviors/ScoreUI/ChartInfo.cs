using Godot;
using Winithm.Core;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class ChartInfo : Control
  {
    public struct LastState
    {
      public string DifficultText;
      public Color TextColor, TextOutLineColor, CompBackgroundColor;
    }

    [Export] public Vector2 ScreenSize = Constants.Visual.DESIGN_RESOLUTION;
    [Export] public string DifficultText = "Info: 5";
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;
    [Export] public Color CompBackgroundColor = Colors.Gray;

    private LastState _lastState = new LastState();

    private Label _difficult;
    private ColorRect _background;

    public override void _Ready()
    {
      _difficult = GetNode<Label>("Difficult");
      _background = GetNodeOrNull<ColorRect>("Background");
      if (_background != null && _background.Material != null)
      {
        _background.Material = (Material)_background.Material.Duplicate();
      }
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
        || TextOutLineColor != _lastState.TextOutLineColor
        || CompBackgroundColor != _lastState.CompBackgroundColor;
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
      
      if (_background != null && _background.Material is ShaderMaterial mat)
      {
        mat.SetShaderParam("bg_color", CompBackgroundColor);
        mat.SetShaderParam("stripe_color", new Color(0f, 0f, 0f, 0f)); // Transparent
      }

      _lastState.TextColor = TextColor;
      _lastState.TextOutLineColor = TextOutLineColor;
      _lastState.CompBackgroundColor = CompBackgroundColor;
    }

    private void UpdateInfo()
    {
      if (_difficult != null)
      {
        _difficult.Text = DifficultText;
        
        if (_background != null)
        {
          float textWidth = _difficult.GetFont("font").GetStringSize(_difficult.Text).x;
          float textHeight = _difficult.RectSize.y;
          float bgWidth = textWidth + 10f; // 5px padding on each side
          float bgHeight = textHeight + 10f; // 5px padding top and bottom
          
          // Right aligned label ends at _difficult.MarginRight. We position the background based on that.
          float rightEdge = _difficult.MarginRight;
          float leftEdge = rightEdge - textWidth;
          float topEdge = _difficult.MarginTop;
          
          _background.RectPosition = new Vector2(leftEdge - 5f, topEdge - 5f);
          _background.RectSize = new Vector2(bgWidth, bgHeight);
          
          if (_background.Material is ShaderMaterial mat)
          {
            mat.SetShaderParam("rect_size", _background.RectSize);
          }
        }
      }

      _lastState.DifficultText = DifficultText;
    }
  }
}
