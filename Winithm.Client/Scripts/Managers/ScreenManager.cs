using Godot;

namespace Winithm.Client.Managers
{
  public class ScreenManager : Node
  {
    public override void _Ready()
    {
      OS.WindowPosition = Vector2.Zero;
    }
  }
}