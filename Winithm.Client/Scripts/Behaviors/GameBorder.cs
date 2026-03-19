using Godot;
using Winithm.Client.Managers;

namespace Winithm.Client.Behaviors
{
  /// <summary>
  /// A simple UI overlay that draws a decorative border around the entire game window
  /// when the application is running in Windowed mode.
  /// </summary>
  public class GameBorder : Control
  {
    public override void _Process(float delta)
    {
      Update();
    }

    public override void _Draw()
    {
      if (ScreenManager.Instance != null && ScreenManager.Instance.CurrentDisplayMode == AppDisplayMode.Windowed)
      {
        // Draw a sleek white border slightly inside the window bounds
        // Thickness of 2.0 with anti-aliasing (aa=true)
        DrawRect(new Rect2(Vector2.Zero, RectSize), new Color(1, 1, 1, 0.4f), false, 2.0f, true);
      }
    }
  }
}
