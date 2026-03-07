using Godot;

public class ScreenManager : Node
{
  public override void _Ready()
  {
    // OS.WindowSize = OS.GetScreenSize();
    OS.WindowPosition = Vector2.Zero;
  }
}