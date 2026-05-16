using Godot;
using System;
using System.Runtime.InteropServices;

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

    private DesktopDisplayMode _displayMode = DesktopDisplayMode.Windowed;

    [Export]
    public DesktopDisplayMode DisplayMode
    {
      get => _displayMode;
      set => SetDesktopDisplayMode(value);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    public override void _Ready()
    {
      Instance = this;
      DisplayMode = DesktopDisplayMode.Windowed;
    }

    private Rect2 GetWorkArea()
    {
      if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
      {
        POINT pt = new POINT { x = (int)OS.WindowPosition.x, y = (int)OS.WindowPosition.y };
        IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        if (hMonitor != IntPtr.Zero)
        {
          MONITORINFO mi = new MONITORINFO();
          mi.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
          if (GetMonitorInfo(hMonitor, ref mi))
          {
            return new Rect2(
              mi.rcWork.left, 
              mi.rcWork.top, 
              mi.rcWork.right - mi.rcWork.left, 
              mi.rcWork.bottom - mi.rcWork.top
            );
          }
        }
      }
      
      Rect2 safeArea = OS.GetWindowSafeArea();
      if (safeArea.Size != Vector2.Zero)
      {
          return safeArea;
      }
      
      return new Rect2(Vector2.Zero, OS.GetScreenSize());
    }

    public void SetDesktopDisplayMode(DesktopDisplayMode mode)
    {
      _displayMode = mode;
      switch (mode)
      {
        case DesktopDisplayMode.FullScreen:
          Rect2 workArea = GetWorkArea();
          OS.WindowPosition = workArea.Position;
          OS.WindowSize = workArea.Size;
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
      SetDesktopDisplayMode(_displayMode == DesktopDisplayMode.FullScreen
          ? DesktopDisplayMode.Windowed
          : DesktopDisplayMode.FullScreen);
    }
  }
}