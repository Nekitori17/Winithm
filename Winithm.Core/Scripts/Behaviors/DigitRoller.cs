using Godot;

namespace Winithm.Core.Behaviors
{
  public class DigitRoller : Control
  {
    private VBoxContainer _vbox;
    private int _targetDigit = 0;

    private float _animTimer = 0f;
    private float _animDuration = 0f;

    private float _startDigitValue = 0f;
    private float _currentDigitValue = 0f;

    public override void _Ready()
    {
      _vbox = GetNodeOrNull<VBoxContainer>("VBoxContainer");
    }

    public override void _Process(float delta)
    {
      if (_animTimer > 0f && _vbox != null)
      {
        _animTimer -= delta;
        if (_animTimer <= 0f)
        {
          _animTimer = 0f;
          _currentDigitValue = _targetDigit;
        }
        else
        {
          float t = 1f - (_animTimer / _animDuration);
          t = t * t * (3f - 2f * t); // smoothstep
          _currentDigitValue = Mathf.Lerp(_startDigitValue, _targetDigit, t);
        }
      }

      if (_vbox != null)
      {
        int index = Mathf.Clamp(Mathf.FloorToInt(_currentDigitValue), 0, 9);
        int nextIndex = Mathf.Clamp(index + 1, 0, 9);
        float remainder = _currentDigitValue - index;

        Label lbl1 = _vbox.GetNodeOrNull<Label>(index.ToString());
        Label lbl2 = _vbox.GetNodeOrNull<Label>(nextIndex.ToString());

        if (lbl1 != null && lbl2 != null)
        {
          float y1 = lbl1.RectPosition.y;
          float y2 = lbl2.RectPosition.y;
          float y = Mathf.Lerp(y1, y2, remainder);
          _vbox.RectPosition = new Vector2(_vbox.RectPosition.x, -y);
        }
      }
    }

    public void SetDigit(int digit, bool instant)
    {
      if (digit < 0) digit = 0;
      if (digit > 9) digit = 9;

      if (_targetDigit == digit && _animTimer <= 0f) return;

      _targetDigit = digit;

      if (instant)
      {
        _animTimer = 0f;
        _currentDigitValue = digit;
      }
      else
      {
        _startDigitValue = _currentDigitValue;
        float distance = Mathf.Abs(_targetDigit - _startDigitValue);
        _animDuration = Mathf.Max(0.01f, distance * 0.1f);
        _animTimer = _animDuration;
      }
    }

    public void UpdateColor(Color textColor, Color outlineColor)
    {
      if (_vbox == null) return;
      foreach (Node child in _vbox.GetChildren())
      {
        if (child is Label lbl)
        {
          lbl.AddColorOverride("font_color", textColor);
          lbl.AddColorOverride("font_outline_modulate", outlineColor);
        }
      }
    }
  }
}