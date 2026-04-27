using Godot;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class SongInfo : Control
  {
    public struct LastState
    {
      public Vector2 ScreenSize;
      public string SongName;
      public float BPM;
      public Color TextColor, TextOutLineColor;
      public Texture SongIcon;
    }

    [Export] public Vector2 ScreenSize = Constants.Visual.DESIGN_RESOLUTION;
    [Export] public string SongName = "Song Name";
    [Export] public float BPM = 120f;
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;
    [Export] public Texture SongIcon = 
      GD.Load<Texture>("res://Winithm.Core/Resources/Textures/song_placeholder_image.png");

    private LastState _lastState = new LastState();

    
    public void UpdateVisual()
    {
      bool isLayoutDirty = ScreenSize != _lastState.ScreenSize;
    }

    private void UpdateIcon()
    {
      
    }
    
    private void UpdateInfo()
    {
      
    }
  }
}