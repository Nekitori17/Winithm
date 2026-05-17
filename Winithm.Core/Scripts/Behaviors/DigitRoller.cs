using Godot;

namespace Winithm.Core.Behaviors
{
  public class DigitRoller : Control
  {
    private Control _container;
    private Label _templateLabel;
    private int _targetDigit = 0;

    private float _currentY = 0f;
    private float _targetY = 0f;
    private const float ITEM_HEIGHT = 41f;

    public override void _Ready()
    {
      _container = GetNodeOrNull<Control>("VBoxContainer");
      if (_container != null && _container.GetChildCount() > 0)
      {
        var firstChild = _container.GetChild<Label>(0);
        _templateLabel = (Label)firstChild.Duplicate();

        // Clean up any extra children if they exist
        for (int i = _container.GetChildCount() - 1; i > 0; i--)
        {
          var child = _container.GetChild(i);
          _container.RemoveChild(child);
          child.QueueFree();
        }
      }
    }

    public override void _Process(float delta)
    {
      if (_container == null) return;

      // Smooth damp towards the target Y position
      if (Mathf.Abs(_currentY - _targetY) > 0.01f)
      {
        _currentY = Mathf.Lerp(_currentY, _targetY, 15f * delta);
      }
      else
      {
        _currentY = _targetY;
      }

      _container.RectPosition = new Vector2(_container.RectPosition.x, _currentY);

      // Culling logic: If we've passed at least one full item height, cull the top item
      while (_currentY <= -ITEM_HEIGHT && _container.GetChildCount() > 1)
      {
        var topChild = _container.GetChild<Control>(0);
        _container.RemoveChild(topChild);
        topChild.QueueFree();

        // Shift all remaining children UP by ITEM_HEIGHT in their local space
        foreach (Control child in _container.GetChildren())
        {
          child.RectPosition = new Vector2(child.RectPosition.x, child.RectPosition.y - ITEM_HEIGHT);
        }

        // Shift the container DOWN by ITEM_HEIGHT to perfectly counteract the visual jump
        _currentY += ITEM_HEIGHT;
        _targetY += ITEM_HEIGHT;
        _container.RectPosition = new Vector2(_container.RectPosition.x, _currentY);
      }
    }

    public void SetDigit(int digit, bool instant)
    {
      if (digit < 0) digit = 0;
      if (digit > 9) digit = 9;

      if (_targetDigit == digit && !instant) return;
      _targetDigit = digit;

      if (_container == null || _templateLabel == null) return;

      if (instant)
      {
        // Remove all children except the last one
        for (int i = 0; i < _container.GetChildCount() - 1; i++)
        {
          var child = _container.GetChild(0);
          _container.RemoveChild(child);
          child.QueueFree();
        }

        var remainingChild = _container.GetChild<Label>(0);
        remainingChild.Text = digit.ToString();
        remainingChild.RectPosition = new Vector2(remainingChild.RectPosition.x, 0);

        _currentY = 0f;
        _targetY = 0f;
        _container.RectPosition = new Vector2(_container.RectPosition.x, 0);
      }
      else
      {
        // Add a new label at the bottom
        Label newLabel = (Label)_templateLabel.Duplicate();
        newLabel.Text = digit.ToString();
        
        int currentIndex = _container.GetChildCount();
        
        // Ensure new label has correct Y position relative to the previous one
        float newLocalY = 0f;
        if (currentIndex > 0)
        {
          var lastChild = _container.GetChild<Control>(currentIndex - 1);
          newLocalY = lastChild.RectPosition.y + ITEM_HEIGHT;
        }

        newLabel.RectPosition = new Vector2(newLabel.RectPosition.x, newLocalY);
        _container.AddChild(newLabel);

        // Move the target Y up by one item height to scroll to the newly added label
        _targetY -= ITEM_HEIGHT;
      }
    }

    public void UpdateColor(Color textColor, Color outlineColor)
    {
      if (_templateLabel != null)
      {
        _templateLabel.AddColorOverride("font_color", textColor);
        _templateLabel.AddColorOverride("font_outline_modulate", outlineColor);
      }

      if (_container == null) return;
      foreach (Node child in _container.GetChildren())
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