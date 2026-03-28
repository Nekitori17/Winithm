using Godot;

namespace Winithm.Client.Behaviors.Gameplay
{
  public class PlayerArea : Control
  {
    public override void _Draw()
    {
      if (PlayerWrapper.Instance != null && PlayerWrapper.Instance.AspectMode == GameplayAspectMode.Ratio16_9)
      {
        DrawRect(new Rect2(Vector2.Zero, RectSize), new Color(0, 1, 1, 1f), false, 2.0f, true); 
      }
    }
  }
}