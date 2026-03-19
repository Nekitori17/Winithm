using Godot;
using System;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Managers;

namespace Winithm.Client.Gameplay
{
  public class Player : Node2D
  {
    private TimeManager _timeManager;
    private MusicPlayer _musicPlayer;
    private WindowManager _windowManager;

    public override void _Ready()
    {
      // Setup Managers
      _timeManager = new TimeManager();
      _timeManager.Name = "TimeManager";
      AddChild(_timeManager);

      _musicPlayer = new MusicPlayer();
      _musicPlayer.Name = "MusicPlayer";
      AddChild(_musicPlayer);

      _windowManager = new WindowManager();
      _windowManager.Name = "WindowManager";
      AddChild(_windowManager);

      // Hide the test label
      var testLabel = GetNodeOrNull<Label>("UI/Test");
      if (testLabel != null) testLabel.Visible = false;

      // Load Level Data
      string folderPath = "res://Winithm.Assets/Levels/frizka.allMyFellas";
      string chartFileName = "info";

      // WinithmChartIO assumes local OS paths if not starting with res://, but let's try passing the absolute path or checking WinithmChartIO
      string globalFolderPath = ProjectSettings.GlobalizePath(folderPath);
      ChartData chart = WinithmChartIO.LoadLevel(globalFolderPath, chartFileName);

      if (chart == null)
      {
        GD.PushError($"Failed to load chart from {globalFolderPath}");
        return;
      }

      // Initialize Managers
      _timeManager.Initialize(chart.Resources.BPMList);
      _windowManager.Initialize(chart.Windows);

      // Load Audio
      AudioStream audio = ResourceLoader.Load<AudioStream>($"{folderPath}/{chart.Resources.SongPath}");
      if (audio != null)
      {
        _musicPlayer.Stream = audio;
      }
      else
      {
        GD.PushError("Failed to load song.mp3");
      }

      // Start Playback
      _timeManager.Play();
      _musicPlayer.PlayFrom(0f);
    }
  }
}
