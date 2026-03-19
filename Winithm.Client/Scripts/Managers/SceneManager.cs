using Godot;

namespace Winithm.Client.Managers
{
  public class SceneManager : Node
  {
    private Control _sceneRoot;
    public override void _Ready()
    {
      _sceneRoot = GetNode<Control>("SceneRoot");
    }
  }
}