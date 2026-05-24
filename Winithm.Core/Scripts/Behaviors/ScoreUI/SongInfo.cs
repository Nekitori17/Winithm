using Godot;
using Winithm.Core;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class SongInfo : Control
  {
    public struct LastState
    {
      public Vector2 IconCenter;
      public Color TextColor, TextOutLineColor, CompBackgroundColor;
      public string SongName;
      public float BPM, IconSize;
      public Texture SongIcon;
    }

    [Export] public Vector2 ScreenSize = Constants.Visual.DESIGN_RESOLUTION;
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;
    [Export] public Color CompBackgroundColor = Colors.Gray;
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
    private ColorRect _background;
    private AtlasTexture _atlasTex;

    public override void _Ready()
    {
      _icon = GetNode<TextureRect>("Icon");
      _name = GetNode<Label>("Name");
      _bpm = GetNode<Label>("BPM");
      _background = GetNodeOrNull<ColorRect>("Background");
      
      if (_background != null && _background.Material != null)
      {
        _background.Material = (Material)_background.Material.Duplicate();
      }

      UpdateVisual();
    }

    public void UpdateVisual()
    {
      bool isColorDirty = 
        TextColor != _lastState.TextColor 
        || TextOutLineColor != _lastState.TextOutLineColor
        || CompBackgroundColor != _lastState.CompBackgroundColor;

      bool isInfoDirty = SongName != _lastState.SongName || BPM != _lastState.BPM;                 
      bool isIconDirty = SongIcon != _lastState.SongIcon ||
                         IconCenter != _lastState.IconCenter ||
                         IconSize != _lastState.IconSize;

      if (isColorDirty) UpdateColor();
      if (isInfoDirty) UpdateInfo();
      if (isIconDirty) UpdateIcon();
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
      
      if (_background != null && _background.Material is ShaderMaterial mat)
      {
        mat.SetShaderParam("bg_color", CompBackgroundColor);
        mat.SetShaderParam("stripe_color", new Color(0f, 0f, 0f, 0f)); // Transparent
      }

      _lastState.TextColor = TextColor;
      _lastState.TextOutLineColor = TextOutLineColor;
      _lastState.CompBackgroundColor = CompBackgroundColor;
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

      if (_background != null && _name != null && _bpm != null)
      {
        float nameWidth = _name.GetFont("font").GetStringSize(_name.Text).x;
        float bpmWidth = _bpm.GetFont("font").GetStringSize(_bpm.Text).x;
        float maxTextWidth = Mathf.Max(nameWidth, bpmWidth);
        
        float textStartX = Mathf.Min(_name.MarginLeft, _bpm.MarginLeft);
        float bgWidth = maxTextWidth + 10f; // 5px padding on left and right
        
        // Height matches song icon
        _background.RectPosition = new Vector2(textStartX - 5f, _icon != null ? _icon.MarginTop : -65f);
        _background.RectSize = new Vector2(bgWidth, _icon != null ? _icon.RectSize.y : 50f);
        
        if (_background.Material is ShaderMaterial mat)
        {
          mat.SetShaderParam("rect_size", _background.RectSize);
        }
      }

      _lastState.SongName = SongName;
      _lastState.BPM = BPM;
    }
  }
}