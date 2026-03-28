using Godot;
using System.IO;
using Winithm.Core.Managers;
using Winithm.Core.Common;
using Winithm.Client.Managers;

namespace Winithm.Client.Behaviors.Gameplay
{
  public class Player : Control
  {
    private TimeManager _timeManager;
    private ClientWindowManager _windowManager;
    private GroupManager _groupManager;
    private ThemeChannelManager _themeManager;

    public override void _Ready()
    {
      _timeManager = new TimeManager() { Name = "TimeManager" };
      AddChild(_timeManager);

      _themeManager = new ThemeChannelManager() { Name = "ThemeChannelManager" };
      AddChild(_themeManager);

      _groupManager = new GroupManager() { Name = "GroupManager" };
      AddChild(_groupManager);

      _windowManager = GetNodeOrNull<ClientWindowManager>("WindowManager");
      if (_windowManager == null)
      {
        _windowManager = new ClientWindowManager() { Name = "WindowManager" };
        AddChild(_windowManager);
      }

      _windowManager.InjectManagers(_timeManager, _groupManager, _themeManager);

      var parent = GetParent<Control>();
      if (parent != null)
        parent.Connect("item_rect_changed", this, nameof(OnParentResized));
      OnParentResized();

      LoadDemoLevel();
    }

    private async void LoadDemoLevel()
    {
      string resFolderPath = "res://Winithm.Assets/Levels/frizka.allMyFellas";
      string chartName = "info"; 
      string globalPath = ProjectSettings.GlobalizePath(resFolderPath);
      
      var chartData = WinithmChartIO.LoadLevel(globalPath, chartName);
      if (chartData == null)
      {
         GD.PrintErr("Failed to load level data.");
         return;
      }

      _timeManager.BPMList = chartData.Resources.BPMList;
      _timeManager.PreComputeBPMList();

      if (!string.IsNullOrEmpty(chartData.Resources.SongPath))
      {
         string songPath = resFolderPath + "/" + chartData.Resources.SongPath;
         var audioStream = GD.Load<AudioStream>(songPath);
         _timeManager.Stream = audioStream;
      }

      _themeManager.LoadThemeChannels(chartData.ThemeChannels);
      _groupManager.LoadGroups(chartData.Groups);
      _windowManager.LoadWindows(chartData.Windows);

      // Delay to ensure the Godot splash screen and rendering initialize cleanly.
      await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

      _timeManager.Play();
    }

    private void OnParentResized()
    {
      if (_windowManager == null) return;
      var parent = GetParent<Control>();
      if (parent == null) return;
      
      _windowManager.GameplayAreaSize = parent.RectSize;
      _windowManager.ViewportSize = OS.WindowSize; // Kích thước màn hình máy tính thực tế
    }
  }
}
