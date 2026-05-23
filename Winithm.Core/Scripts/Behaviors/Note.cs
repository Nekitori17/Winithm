using Godot;
using System;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;
using Winithm.Core.Managers;

namespace Winithm.Core.Behaviors
{
  public class Note : Node2D, IPoolable
  {
    // --- Dirty tracking ---
    // Stores the last state to avoid redundant visual updates (dirty tracking)
    struct NoteState
    {
      public Vector2 PlayerAreaSize;
      public float Width, BodyHeight, NoteSize;
    }
    private NoteState _lastState = new NoteState();

    // --- Child references (assigned in _Ready) ---
    // References to scene nodes assigned during initialization
    private Node2D _headContainer;
    private NinePatchRect _headBase;
    private TextureRect _headOverlay;
    private Node2D _bodyContainer;
    private NinePatchRect _bodyBase;

    // --- Properties set by NoteManager ---
    // Configurable properties typically managed by NoteManager
    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);
    [Export] public float Width = 300f;
    [Export] public NoteType Type;
    [Export] public float NoteSize = 1f;
    [Export] public float BodyHeight = 0f;
    public ResourcePack ResourcePack = ResourcePackManager.Instance.GetActiveResourcePack();

    public static readonly float BODY_TO_HEAD_WIDTH_OFFSET = 0.015f;
    public static readonly float NOTE_HEAD_HEIGHT_RATIO = 0.0175f;
    public static readonly float NOTE_OVERLAY_RATIO = 1.2f;

    // Initialize node references and perform initial visual update
    public override void _Ready()
    {
      _headContainer = GetNode<Node2D>("Head");
      _headBase = GetNode<NinePatchRect>("Head/Base");
      _headOverlay = GetNode<TextureRect>("Head/Overlay");

      _bodyContainer = GetNode<Node2D>("Body");
      _bodyBase = GetNode<NinePatchRect>("Body/Base");

      UpdateVisual();
    }

    // Logic to execute when the note is retrieved from the object pool
    public void OnSpawn(){ }

    public void OnDespawn() { }

    private Texture GetTextureSafe(NoteType type, NotePart part)
    {
      if (ResourcePack.TEX != null && ResourcePack.TEX.TryGetValue(type, out var parts))
      {
        if (parts.TryGetValue(part, out var tex))
        {
          return tex;
        }
      }
      return null;
    }

    public void SetNoteType(NoteType type, ResourcePack resourcePack)
    {
      bool isDirty = Type != type || !ReferenceEquals(ResourcePack, resourcePack);
      if (!isDirty) return;

      Type = type;
      ResourcePack = resourcePack;

      _bodyContainer.Visible = Type == NoteType.Hold;

      _headBase.PatchMarginLeft = ResourcePack.Config.NinePatchHeadMarginH;
      _headBase.PatchMarginRight = ResourcePack.Config.NinePatchHeadMarginH;
      _headBase.PatchMarginTop = 0;
      _headBase.PatchMarginBottom = 0;

      _bodyBase.PatchMarginLeft = ResourcePack.Config.NinePatchBodyMarginH;
      _bodyBase.PatchMarginRight = ResourcePack.Config.NinePatchBodyMarginH;
      _bodyBase.PatchMarginTop = ResourcePack.Config.NinePatchBodyMarginV;
      _bodyBase.PatchMarginBottom = ResourcePack.Config.NinePatchBodyMarginV;

      NoteType headType = Type == NoteType.Hold ? NoteType.Tap : Type;
      
      _headBase.Texture = GetTextureSafe(headType, NotePart.Base);
      _headOverlay.Texture = GetTextureSafe(headType, NotePart.Overlay);
      
      if (Type == NoteType.Hold)
      {
        _bodyBase.Texture = GetTextureSafe(NoteType.Hold, NotePart.Base);
      }
      
      // Force update visual since texture changed
      _lastState = new NoteState();
      UpdateVisual();
    }

    public void SetNoteHighlighting(bool active)
    {
      if (Material is ShaderMaterial shaderMaterial)
      {
        shaderMaterial.SetShaderParam("is_highlighted", active);
        shaderMaterial.SetShaderParam("glow_color", ResourcePack.Config.HighlightColor);
      }
    }

    // Recalculates sizes and positions of all components based on current properties
    public void UpdateVisual()
    {
      float minScale = Math.Min(PlayerAreaSize.x, PlayerAreaSize.y);
      float headH = NoteSize * minScale * NOTE_HEAD_HEIGHT_RATIO;
      float headW = Math.Max(Width, headH * 2f);

      float headScale = 1f;
      if (_headBase.Texture != null && _headBase.Texture.GetSize().y > 0)
      {
         headScale = headH / _headBase.Texture.GetSize().y;
      }

      bool headDirty =
        PlayerAreaSize != _lastState.PlayerAreaSize ||
        Width != _lastState.Width ||
        NoteSize != _lastState.NoteSize;

      bool bodyDirty =
        PlayerAreaSize != _lastState.PlayerAreaSize ||
        Width != _lastState.Width ||
        BodyHeight != _lastState.BodyHeight;

      if (headDirty)
      {
        // Update head component layout
        _headContainer.Position = new Vector2(-headW / 2f, -headH);

        if (_headBase.Texture != null)
        {
          _headBase.RectScale = new Vector2(headScale, headScale);
          _headBase.RectSize = new Vector2(headW / headScale, _headBase.Texture.GetSize().y);
          _headBase.RectPosition = Vector2.Zero;
        }

        if (_headOverlay.Texture != null)
        {
          float overlaySize = headH * NOTE_OVERLAY_RATIO;
          float texW = _headOverlay.Texture.GetSize().x;
          float texH = _headOverlay.Texture.GetSize().y;
          
          _headOverlay.RectScale = new Vector2(texW > 0 ? overlaySize / texW : 1f, texH > 0 ? overlaySize / texH : 1f);
          _headOverlay.RectSize = new Vector2(texW, texH);
          _headOverlay.RectPosition = new Vector2(headW / 2f - overlaySize / 2f, headH / 2f - overlaySize / 2f);
        }
      }

      if (bodyDirty)
      {
        // Update body component layout (for Hold notes)
        float bodyWidthOffset = minScale * BODY_TO_HEAD_WIDTH_OFFSET;
        float bodyW = Math.Max(headW - bodyWidthOffset, headH * 2f);

        _bodyContainer.Position = new Vector2(-bodyW / 2f, -BodyHeight - headH);

        if (_bodyBase.Texture != null)
        {
          _bodyBase.RectScale = new Vector2(headScale, headScale);
          _bodyBase.RectSize = new Vector2(headScale > 0 ? bodyW / headScale : bodyW, headScale > 0 ? BodyHeight / headScale : BodyHeight);
          _bodyBase.RectPosition = Vector2.Zero;
        }
      }

      // Save current state for next dirty check
      _lastState.PlayerAreaSize = PlayerAreaSize;
      _lastState.Width = Width;
      _lastState.NoteSize = NoteSize;
      _lastState.BodyHeight = BodyHeight;
    }
  }
}