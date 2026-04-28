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
    private Sprite _headLeft;
    private Sprite _headCenter;
    private Sprite _headRight;
    private Sprite _headOverlay;
    private Node2D _bodyContainer;
    private Sprite _bodyLeft;
    private Sprite _bodyCenter;
    private Sprite _bodyRight;

    // --- Properties set by NoteManager ---
    // Configurable properties typically managed by NoteManager
    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);
    [Export] public float Width = 300f;
    [Export] public NoteType Type;
    [Export] public float NoteSize = 1f;
    [Export] public float BodyHeight = 0f;
    [Export] public ResourcePack ResourcePack = ResourcePackManager.Instance.GetActiveResourcePack();

    public static readonly float BODY_TO_HEAD_RATIO = 0.9f;
    public static readonly float NOTE_HEAD_HEIGHT_RATIO = 0.025f;
    public static readonly float NOTE_OVERLAY_RATIO = 1.2f;

    // Initialize node references and perform initial visual update
    public override void _Ready()
    {
      _headContainer = GetNode<Node2D>("Head");
      _headLeft = GetNode<Sprite>("Head/Left");
      _headCenter = GetNode<Sprite>("Head/Center");
      _headRight = GetNode<Sprite>("Head/Right");
      _headOverlay = GetNode<Sprite>("Head/CenterOverlay");

      _bodyContainer = GetNode<Node2D>("Body");
      _bodyLeft = GetNode<Sprite>("Body/Left");
      _bodyCenter = GetNode<Sprite>("Body/Center");
      _bodyRight = GetNode<Sprite>("Body/Right");

      UpdateVisual();
    }

    // Logic to execute when the note is retrieved from the object pool
    public void OnSpawn(){ }

    public void OnDespawn() { }

    public void SetNoteType(NoteType type, ResourcePack resourcePack)
    {
      bool isDirty = Type != type || !ReferenceEquals(ResourcePack, resourcePack);
      if (!isDirty) return;

      Type = type;
      ResourcePack = resourcePack;

      _bodyContainer.Visible = Type == NoteType.Hold;

      switch (Type)
      {
        case NoteType.Tap:
          _headLeft.Texture = ResourcePack.TEX[NoteType.Tap][NotePart.Left];
          _headCenter.Texture = ResourcePack.TEX[NoteType.Tap][NotePart.Center];
          _headRight.Texture = ResourcePack.TEX[NoteType.Tap][NotePart.Right];
          _headOverlay.Texture = ResourcePack.TEX[NoteType.Tap][NotePart.Overlay];
          break;
        case NoteType.Hold:
          _headLeft.Texture = ResourcePack.TEX[NoteType.Tap][NotePart.Left];
          _headCenter.Texture = ResourcePack.TEX[NoteType.Tap][NotePart.Center];
          _headRight.Texture = ResourcePack.TEX[NoteType.Tap][NotePart.Right];
          _headOverlay.Texture = ResourcePack.TEX[NoteType.Tap][NotePart.Overlay];

          _bodyLeft.Texture = ResourcePack.TEX[NoteType.Hold][NotePart.Left];
          _bodyCenter.Texture = ResourcePack.TEX[NoteType.Hold][NotePart.Center];
          _bodyRight.Texture = ResourcePack.TEX[NoteType.Hold][NotePart.Right];
          break;
        case NoteType.Drag:
          _headLeft.Texture = ResourcePack.TEX[NoteType.Drag][NotePart.Left];
          _headCenter.Texture = ResourcePack.TEX[NoteType.Drag][NotePart.Center];
          _headRight.Texture = ResourcePack.TEX[NoteType.Drag][NotePart.Right];
          _headOverlay.Texture = ResourcePack.TEX[NoteType.Drag][NotePart.Overlay];
          break;
        case NoteType.Focus:
          _headLeft.Texture = ResourcePack.TEX[NoteType.Focus][NotePart.Left];
          _headCenter.Texture = ResourcePack.TEX[NoteType.Focus][NotePart.Center];
          _headRight.Texture = ResourcePack.TEX[NoteType.Focus][NotePart.Right];
          _headOverlay.Texture = ResourcePack.TEX[NoteType.Focus][NotePart.Overlay];
          break;
        case NoteType.Close:
          _headLeft.Texture = ResourcePack.TEX[NoteType.Close][NotePart.Left];
          _headCenter.Texture = ResourcePack.TEX[NoteType.Close][NotePart.Center];
          _headRight.Texture = ResourcePack.TEX[NoteType.Close][NotePart.Right];
          _headOverlay.Texture = ResourcePack.TEX[NoteType.Close][NotePart.Overlay];
          break;
      }
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
      float headH = NoteSize * Math.Min(PlayerAreaSize.x, PlayerAreaSize.y) * NOTE_HEAD_HEIGHT_RATIO;
      float headW = Math.Max(Width, headH * 2f);
      float headCW = headW - headH * 2f;

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

        _headLeft.Scale = PixelScale(_headLeft, headH, headH);
        _headLeft.Position = new Vector2(0f, 0f);

        _headCenter.Scale = PixelScale(_headCenter, headCW, headH);
        _headCenter.Position = new Vector2(headH, 0f);

        _headRight.Scale = PixelScale(_headRight, headH, headH);
        _headRight.Position = new Vector2(headW - headH, 0f);

        float overlaySize = headH * NOTE_OVERLAY_RATIO;
        _headOverlay.Scale = PixelScale(_headOverlay, overlaySize, overlaySize);
        _headOverlay.Position = new Vector2(headW / 2f - overlaySize / 2f, headH / 2f - overlaySize / 2f);
      }

      if (bodyDirty)
      {
        // Update body component layout (for Hold notes)
        float bodyW = Math.Max(headW * BODY_TO_HEAD_RATIO, headH * 2f);
        float bodyCW = bodyW - headH * 2f;

        _bodyContainer.Position = new Vector2(-bodyW / 2f, -BodyHeight - headH);

        _bodyLeft.Scale = PixelScale(_bodyLeft, headH, BodyHeight);
        _bodyLeft.Position = new Vector2(0f, 0f);

        _bodyCenter.Scale = PixelScale(_bodyCenter, bodyCW, BodyHeight);
        _bodyCenter.Position = new Vector2(headH, 0f);

        _bodyRight.Scale = PixelScale(_bodyRight, headH, BodyHeight);
        _bodyRight.Position = new Vector2(bodyW - headH, 0f);
      }

      // Save current state for next dirty check
      _lastState.PlayerAreaSize = PlayerAreaSize;
      _lastState.Width = Width;
      _lastState.NoteSize = NoteSize;
      _lastState.BodyHeight = BodyHeight;
    }

    // Converts desired pixel dimensions to a scale vector relative to the sprite's texture size
    private static Vector2 PixelScale(Sprite sprite, float pixelW, float pixelH)
    {
      Texture tex = sprite.Texture;
      if (tex == null) return Vector2.One;
      Vector2 size = tex.GetSize();
      return new Vector2(
        size.x > 0 ? pixelW / size.x : 1f,
        size.y > 0 ? pixelH / size.y : 1f
      );
    }
  }
}