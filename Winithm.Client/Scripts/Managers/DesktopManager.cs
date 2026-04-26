using Godot;

namespace Winithm.Client.Managers
{
  public enum DesktopDisplayMode
  {
    FullScreen,
    Windowed
  }

  public class DesktopManager : Node
  {
    public static DesktopManager Instance { get; private set; }

    public DesktopDisplayMode DisplayMode { get; private set; } = DesktopDisplayMode.Windowed;

    public override void _Ready()
    {
      Instance = this;
      SetDesktopDisplayMode(DesktopDisplayMode.Windowed);
      OS.WindowPosition = Vector2.Zero;
    }

    public void SetDesktopDisplayMode(DesktopDisplayMode mode)
    {
      DisplayMode = mode;
      switch (mode)
      {
        case DesktopDisplayMode.FullScreen:
          OS.WindowPosition = Vector2.Zero;
          OS.WindowBorderless = true;
          OS.WindowResizable = false;
          break;
        case DesktopDisplayMode.Windowed:
          OS.WindowFullscreen = false;
          OS.WindowBorderless = false;
          OS.WindowResizable = true;
          break;
      }
    }

    /// <summary>
    /// Optional: Allow toggling explicitly via code/actions
    /// </summary>
    public void ToggleDisplayMode()
    {
      SetDesktopDisplayMode(DisplayMode == DesktopDisplayMode.FullScreen
          ? DesktopDisplayMode.Windowed
          : DesktopDisplayMode.FullScreen);
    }
  }
}