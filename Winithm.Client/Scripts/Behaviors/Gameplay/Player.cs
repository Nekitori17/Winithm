using Godot;
using Winithm.Core.Behaviors;
using Winithm.Client.Managers;

namespace Winithm.Client.Gameplay
{
  public class Player : Control
  {
    private Window _window;

    public override void _Ready()
    {
      _window = GetNode<Window>("Window");

      // Connect to ScreenManager signal
      ScreenManager.Instance.Connect(nameof(ScreenManager.GameWindowSizeChange), this, nameof(OnGameWindowSizeChange));

      // Initial trigger
      OnGameWindowSizeChange(GetViewport().Size);
    }

    private void OnGameWindowSizeChange(Vector2 newSize)
    {
      if (_window == null) return;

      // Update Window ScreenSize (Viewport resolution)
      _window.ScreenSize = newSize;
      _window.UpdateWindow();
    }
  }
}
