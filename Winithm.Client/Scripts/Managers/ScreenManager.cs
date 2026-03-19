using Godot;

namespace Winithm.Client.Managers
{
  public enum AppDisplayMode
  {
    FullScreen,
    Windowed
  }

  public class ScreenManager : Node
  {
    public static ScreenManager Instance { get; private set; }

    public AppDisplayMode CurrentDisplayMode { get; private set; } = AppDisplayMode.Windowed;

    public override void _Ready()
    {
      Instance = this;
      SetAppDisplayMode(AppDisplayMode.FullScreen);
      OS.WindowPosition = Vector2.Zero;
    }

    public void SetAppDisplayMode(AppDisplayMode mode)
    {
      CurrentDisplayMode = mode;
      switch (mode)
      {
        case AppDisplayMode.FullScreen:
          OS.WindowPosition = Vector2.Zero;
          OS.WindowBorderless = true;
          OS.WindowResizable = false;
          break;
        case AppDisplayMode.Windowed:
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
      SetAppDisplayMode(CurrentDisplayMode == AppDisplayMode.FullScreen
          ? AppDisplayMode.Windowed
          : AppDisplayMode.FullScreen);
    }
  }
}