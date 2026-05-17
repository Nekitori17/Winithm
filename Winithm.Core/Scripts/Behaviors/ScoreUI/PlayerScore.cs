using System.Collections.Generic;
using Godot;
using Winithm.Core;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class PlayerScore : Control
  {
    public struct LastState
    {
      public Color TextColor, TextOutLineColor;
    }

    [Export] public Vector2 ScreenSize = Constants.Visual.DESIGN_RESOLUTION;
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;

    private LastState _lastState = new LastState();

    private HBoxContainer _scoreContainer;
    private Label _accuracyLabel;

    public override void _Ready()
    {
      _scoreContainer = GetNodeOrNull<HBoxContainer>("Score");
      _accuracyLabel = GetNodeOrNull<Label>("Accuracy");
      UpdateVisual();
    }

    public override void _Process(float delta)
    {
      // No longer need to manage a queue, DigitRoller handles seamless updates natively
    }

    public void UpdateVisual()
    {
      bool isColorDirty =
        TextColor != _lastState.TextColor
        || TextOutLineColor != _lastState.TextOutLineColor;

      if (isColorDirty) UpdateColor();
    }

    private void UpdateColor()
    {
      if (_accuracyLabel != null)
      {
        _accuracyLabel.AddColorOverride("font_color", TextColor);
        _accuracyLabel.AddColorOverride("font_outline_modulate", TextOutLineColor);
      }

      if (_scoreContainer != null)
      {
        foreach (Node child in _scoreContainer.GetChildren())
        {
          if (child is DigitRoller roller)
          {
            roller.UpdateColor(TextColor, TextOutLineColor);
          }
        }
      }

      _lastState.TextColor = TextColor;
      _lastState.TextOutLineColor = TextOutLineColor;
    }

    public void SetAccuracy(float accuracy)
    {
      _accuracyLabel.Text = $"{accuracy * 100:F2}%";
    }

    public void SetScore(int score, bool instant)
    {
      ApplyScoreToRollers(score, instant);
    }

    private void ApplyScoreToRollers(int score, bool instant)
    {
      if (_scoreContainer == null) return;
      string scoreStr = score.ToString("D7");
      int i = 0;
      foreach (Node child in _scoreContainer.GetChildren())
      {
        if (child is DigitRoller roller && i < 7)
        {
          int digit = scoreStr[i] - '0';
          roller.SetDigit(digit, instant);
          i++;
        }
      }
    }
  }
}