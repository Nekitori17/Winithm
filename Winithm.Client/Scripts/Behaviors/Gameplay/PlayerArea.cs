using Godot;

namespace Winithm.Client.Behaviors.Gameplay
{
  public class PlayerArea : Control
  {
    public override void _Draw()
    {
      DrawRect(new Rect2(Vector2.Zero, RectSize), new Color(0f, 1f, 1f, 0.8f), false, 2f);
    }
  }
}