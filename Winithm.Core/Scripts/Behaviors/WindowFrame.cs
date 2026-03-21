using Godot;

namespace Winithm.Core.Behaviors
{
  public class WindowFrame : Control
  {
    private Window _parent;

    public override void _Ready()
    {
      _parent = GetParent<Window>();
    }

    public override void _Draw()
    {
      if (_parent == null || _parent.Borderless) return;

      Color color = _parent.TitleBarColor;
      color.a = 0.5f;

      DrawRect(new Rect2(Vector2.Zero, RectSize), color, false, _parent._titleBar.RectSize.y * 0.0005f, true);
    }
  }
}
