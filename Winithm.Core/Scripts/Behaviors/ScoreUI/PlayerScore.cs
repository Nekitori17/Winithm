using System.Collections.Generic;
using Godot;
using Winithm.Core.Constants;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class PlayerScore : Control
  {
    public struct LastState
    {
      public Vector2 ScreenSize;
      public Color TextColor, TextOutLineColor;
    }

    [Export] public Vector2 ScreenSize = Visual.DESIGN_RESOLUTION;
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;

    private LastState _lastState = new LastState();

    private HBoxContainer _scoreContainer;
    private Label _accuracyLabel;

    private readonly Vector2 BASE_SCORE_POS = new Vector2(-25f, 25f);
    private readonly Vector2 BASE_ACCURACY_POS = new Vector2(-25f, 75f);

    private Queue<int> _scoreQueue = new Queue<int>();
    private float _scoreAnimTimer = 0f;
    private int _currentDisplayedScore = 0;

    public override void _Ready()
    {
      _scoreContainer = GetNodeOrNull<HBoxContainer>("Score");
      _accuracyLabel = GetNodeOrNull<Label>("Accuracy");
      UpdateVisual();
    }

    public override void _Process(float delta)
    {
      UpdateVisual();

      if (_scoreAnimTimer > 0f)
      {
         _scoreAnimTimer -= delta;
      }
      else if (_scoreQueue.Count > 0)
      {
         int nextScore = _scoreQueue.Dequeue();
         
         int maxDiff = 0;
         string currStr = _currentDisplayedScore.ToString("D7");
         string nextStr = nextScore.ToString("D7");
         for (int i = 0; i < 7; i++)
         {
            int diff = Mathf.Abs(currStr[i] - nextStr[i]);
            if (diff > maxDiff) maxDiff = diff;
         }

         _scoreAnimTimer = maxDiff * 0.1f;
         _currentDisplayedScore = nextScore;
         ApplyScoreToRollers(nextScore, false);
      }
    }

    public void UpdateVisual()
    {
      bool isLayoutDirty = ScreenSize != _lastState.ScreenSize;
      bool isColorDirty =
        TextColor != _lastState.TextColor
        || TextOutLineColor != _lastState.TextOutLineColor;

      if (isLayoutDirty) UpdateLayout();
      if (isColorDirty) UpdateColor();
    }

    private void UpdateLayout()
    {
      float viewScale = Mathf.Abs(Mathf.Min(
        ScreenSize.x / Visual.DESIGN_RESOLUTION.x,
        ScreenSize.y / Visual.DESIGN_RESOLUTION.y
      ));

      if (_scoreContainer != null)
      {
         _scoreContainer.RectPosition = BASE_SCORE_POS * viewScale;
         _scoreContainer.RectScale = new Vector2(viewScale, viewScale);
      }

      if (_accuracyLabel != null)
      {
         _accuracyLabel.RectPosition = BASE_ACCURACY_POS * viewScale;
      }

      if (_scoreContainer != null)
      {
         foreach (Node child in _scoreContainer.GetChildren())
         {
            if (child is DigitRoller roller)
            {
               roller.UpdateLayout(viewScale);
            }
         }
      }

      _lastState.ScreenSize = ScreenSize;
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
      if (instant)
      {
         _scoreQueue.Clear();
         _scoreAnimTimer = 0f;
         _currentDisplayedScore = score;
         ApplyScoreToRollers(score, true);
      }
      else
      {
         _scoreQueue.Enqueue(score);
      }
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