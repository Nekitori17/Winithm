using Godot;
using Winithm.Core.Constants;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class SongInfo : Control
  {
    public struct LastState
    {
      public Vector2 ScreenSize, IconCenter;
      public Color TextColor, TextOutLineColor;
      public string SongName;
      public float BPM, IconSize;
      public Texture SongIcon;
    }

    [Export] public Vector2 ScreenSize = Visual.DESIGN_RESOLUTION;
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;
    [Export] public string SongName = "Song Name";
    [Export] public float BPM = 120f;
    [Export] public Texture SongIcon = 
      GD.Load<Texture>("res://Winithm.Core/Resources/Textures/song_placeholder_image.png");
    [Export] public Vector2 IconCenter = new Vector2(0.5f, 0.5f);
    [Export] public float IconSize = 1f;

    private LastState _lastState = new LastState();

    private TextureRect _icon;
    private Label _name;
    private Label _bpm;
    private AtlasTexture _atlasTex;

    private readonly Vector2 BASE_ICON_POS = new Vector2(25f, -100f);
    private readonly Vector2 BASE_ICON_SIZE = new Vector2(75f, 75f);
    private readonly Vector2 BASE_NAME_POS = new Vector2(110f, -100f);
    private readonly Vector2 BASE_BPM_POS = new Vector2(110f, -50f);

    public override void _Ready()
    {
      _icon = GetNode<TextureRect>("Icon");
      _name = GetNode<Label>("Name");
      _bpm = GetNode<Label>("BPM");

      UpdateVisual();
    }

    public void UpdateVisual()
    {
      bool isLayoutDirty = ScreenSize != _lastState.ScreenSize;
      bool isColorDirty = 
        TextColor != _lastState.TextColor 
        || TextOutLineColor != _lastState.TextOutLineColor;

      bool isInfoDirty = SongName != _lastState.SongName || BPM != _lastState.BPM;                 
      bool isIconDirty = SongIcon != _lastState.SongIcon ||
                         IconCenter != _lastState.IconCenter ||
                         IconSize != _lastState.IconSize;

      if (isLayoutDirty) UpdateLayout();
      if (isColorDirty) UpdateColor();
      if (isInfoDirty) UpdateInfo();
      if (isIconDirty) UpdateIcon();
    }

    private void UpdateLayout()
    {
      if (_icon == null || _name == null || _bpm == null) return;

      float viewScale = Mathf.Abs(Mathf.Min(
        ScreenSize.x / Visual.DESIGN_RESOLUTION.x,
        ScreenSize.y / Visual.DESIGN_RESOLUTION.y
      ));

      _icon.RectPosition = BASE_ICON_POS * viewScale;
      _icon.RectSize = BASE_ICON_SIZE * viewScale;

      _name.RectPosition = BASE_NAME_POS * viewScale;
      _bpm.RectPosition = BASE_BPM_POS * viewScale;

      _lastState.ScreenSize = ScreenSize;
    }

    private void UpdateColor()
    {
      if (_name != null)
      {
        _name.AddColorOverride("font_color", TextColor);
        _name.AddColorOverride("font_outline_modulate", TextOutLineColor);
      }
      if (_bpm != null)
      {
        _bpm.AddColorOverride("font_color", TextColor);
        _bpm.AddColorOverride("font_outline_modulate", TextOutLineColor);
      }

      _lastState.TextColor = TextColor;
      _lastState.TextOutLineColor = TextOutLineColor;
    }

    private void UpdateIcon()
    {
      if (_icon != null)
      {
        if (SongIcon != null)
        {
          if (_atlasTex == null) _atlasTex = new AtlasTexture();
          _atlasTex.Atlas = SongIcon;

          Vector2 texSize = SongIcon.GetSize();
          float minDim = Mathf.Min(texSize.x, texSize.y);
          float zoom = Mathf.Max(0.01f, IconSize);
          float cropSize = minDim / zoom;

          Vector2 centerPx = new Vector2(texSize.x * IconCenter.x, texSize.y * IconCenter.y);
          Vector2 topLeft = centerPx - new Vector2(cropSize / 2f, cropSize / 2f);

          _atlasTex.Region = new Rect2(topLeft, new Vector2(cropSize, cropSize));
          _icon.Texture = _atlasTex;
        }
        else
        {
          _icon.Texture = null;
        }
      }
      
      _lastState.SongIcon = SongIcon;
      _lastState.IconCenter = IconCenter;
      _lastState.IconSize = IconSize;
    }

    private void UpdateInfo()
    {
      if (_name != null)
      {
        _name.Text = SongName;
      }
      if (_bpm != null)
      {
        _bpm.Text = $"BPM: {BPM}";
      }

      _lastState.SongName = SongName;
      _lastState.BPM = BPM;
    }
  }
}