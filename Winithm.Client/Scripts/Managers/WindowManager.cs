using Godot;

namespace Winithm.Client.Managers
{
  public class WindowManager : Node
  {
    [Export]
    public Control DesktopEnvironment { get; private set; }
    public override void _Ready()
    {
      DesktopEnvironment = GetNode<Control>("DesktopEnvironment");
    }
  }
}