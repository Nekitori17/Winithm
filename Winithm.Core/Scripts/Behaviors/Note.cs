using Godot;
using System;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Behaviors
{
  public class Note : Control, IPoolable
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
    // References to UI nodes assigned during initialization
    private Control _headContainer;
    private TextureRect _headLeft;
    private TextureRect _headCenter;
    private TextureRect _headRight;
    private TextureRect _headOverlay;
    private Control _bodyContainer;
    private TextureRect _bodyLeft;
    private TextureRect _bodyCenter;
    private TextureRect _bodyRight;

    // --- Properties set by NoteManager ---
    // Configurable properties typically managed by NoteManager
    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);
    [Export] public float Width = 300f;
    [Export] public NoteType Type;
    [Export] public float NoteSize = 1f;
    [Export] public float BodyHeight = 0f;

    public static readonly float HEAD_PADDING = 0.001f;
    public static readonly float BODY_TO_HEAD_RATIO = 0.9f;
    public static readonly float NOTE_HEAD_HEIGHT_RATIO = 0.025f;

    // Initialize node references and perform initial visual update
    public override void _Ready()
    {
      _headContainer = GetNode<Control>("Head");
      _headLeft = GetNode<TextureRect>("Head/Left");
      _headCenter = GetNode<TextureRect>("Head/Center");
      _headRight = GetNode<TextureRect>("Head/Right");
      _headOverlay = GetNode<TextureRect>("Head/CenterOverlay");

      _bodyContainer = GetNode<Control>("Body");
      _bodyLeft = GetNode<TextureRect>("Body/Left");
      _bodyCenter = GetNode<TextureRect>("Body/Center");
      _bodyRight = GetNode<TextureRect>("Body/Right");

      UpdateVisual();
    }

    // Logic to execute when the note is retrieved from the object pool
    public void OnSpawn(){ }

    public void OnDespawn() { }

    public void SetNoteType (NoteType type, bool hightlight = false)
    {
      if (Type == type) return;
      Type = type;

      _bodyContainer.Visible = Type == NoteType.Hold;

      switch (Type)
      {
        case NoteType.Tap:
          break;
        case NoteType.Hold:
          break;
        case NoteType.Drag:
          break;
        case NoteType.Focus:
          break;
        case NoteType.Close:
          break;
      }
      UpdateVisual();
    }

    // Recalculates sizes and positions of all components based on current properties
    public void UpdateVisual()
    {
      float headH = NoteSize * Math.Min(PlayerAreaSize.x, PlayerAreaSize.y) * NOTE_HEAD_HEIGHT_RATIO;
      float headW = Width * (1f - HEAD_PADDING * 2f);
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
        _headContainer.RectSize = new Vector2(headW, headH);
        _headContainer.RectPosition = new Vector2(-headW / 2f, -headH);
        _headContainer.RectPivotOffset = new Vector2(headW / 2f, headH);

        _headLeft.RectSize = new Vector2(headH, headH);
        _headLeft.RectPosition = new Vector2(0f, 0f);

        _headCenter.RectSize = new Vector2(headCW, headH);
        _headCenter.RectPosition = new Vector2(headH, 0f);

        _headRight.RectSize = new Vector2(headH, headH);
        _headRight.RectPosition = new Vector2(headW - headH, 0f);

        _headOverlay.RectSize = new Vector2(headH, headH);
        _headOverlay.RectPosition = new Vector2(headW / 2f - headH / 2f, 0f);
      }

      if (bodyDirty)
      {
        // Update body component layout (for Hold notes)
        float bodyW = headW * BODY_TO_HEAD_RATIO;
        float bodyCW = bodyW - headH * 2f;

        _bodyContainer.RectSize = new Vector2(bodyW, BodyHeight);
        _bodyContainer.RectPosition = new Vector2(-bodyW / 2f, -BodyHeight - headH);
        _bodyContainer.RectPivotOffset = new Vector2(bodyW / 2f, BodyHeight);

        _bodyLeft.RectSize = new Vector2(headH, BodyHeight);
        _bodyLeft.RectPosition = new Vector2(0f, 0f);

        _bodyCenter.RectSize = new Vector2(bodyCW, BodyHeight);
        _bodyCenter.RectPosition = new Vector2(headH, 0f);

        _bodyRight.RectSize = new Vector2(headH, BodyHeight);
        _bodyRight.RectPosition = new Vector2(bodyW - headH, 0f);
      }

      // Save current state for next dirty check
      _lastState.PlayerAreaSize = PlayerAreaSize;
      _lastState.Width = Width;
      _lastState.NoteSize = NoteSize;
      _lastState.BodyHeight = BodyHeight;
    }
  }
}