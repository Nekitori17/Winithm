using Godot;

namespace Winithm.Core.Behaviors
{
  public class WindowFrame : Control
  {
    private Window _parent;

    public override void _Ready()
    {
      _parent = GetParent<Window>();
      Connect("draw", this, nameof(OnDraw));
    }

    private void OnDraw()
    {
      if (_parent == null || _parent.Borderless) return;

      Color color = _parent.TitleBarColor;
      color.a = 0.5f;

      float lineWidth = Mathf.Max(1f, _parent.TitleBarHeight * 0.025f);
      DrawRect(new Rect2(Vector2.Zero, RectSize), color, false, lineWidth, true);
    }
  }
}