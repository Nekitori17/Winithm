using System;
using Godot;
using Winithm.Core.Data;
using Winithm.Core.Managers;

namespace Winithm.Core.Behaviors
{
  public class Note : Control, IPoolable
  {
    // --- Dirty tracking ---
    // Stores the last state to avoid redundant visual updates (dirty tracking)
    struct NoteState
    {
      public Vector2 PlayerAreaSize;
      public float LaneWidth, BodyHeight, NoteSize, Opacity;
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
    [Export] public float LaneWidth = 300f;
    [Export] public NoteType Type;
    [Export] public float NoteSize = 1f;
    [Export] public float BodyHeight = 0f;
    [Export] public float Opacity = 1f;

    public const float HeadPadding = 0.001f;
    public const float BodyToHeadRatio = 0.9f;

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
    public void OnSpawn()
    {
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

    public void OnDespawn() { }

    /// <summary>
    /// Recalculates all child sizes, positions, and rotation.
    /// Call after changing any exported property.
    /// </summary>
    // Recalculates sizes and positions of all components based on current properties
    public void UpdateVisual()
    {
      float headH = NoteSize * Math.Min(PlayerAreaSize.x, PlayerAreaSize.y) * 0.025f;
      float headW = LaneWidth * (1f - HeadPadding * 2f);
      float headCW = headW - headH * 2f;

      bool headDirty =
        PlayerAreaSize != _lastState.PlayerAreaSize ||
        LaneWidth != _lastState.LaneWidth ||
        NoteSize != _lastState.NoteSize ||
        Opacity != _lastState.Opacity;

      bool bodyDirty =
        PlayerAreaSize != _lastState.PlayerAreaSize ||
        LaneWidth != _lastState.LaneWidth ||
        BodyHeight != _lastState.BodyHeight ||
        Opacity != _lastState.Opacity;

      if (headDirty)
      {
        // Update head component layout
        // HeadContainer: pivot at bottom-center so (0,0) of Anchor = note hit point
        _headContainer.RectSize = new Vector2(headW, headH);
        _headContainer.RectPosition = new Vector2(-headW / 2f, -headH);
        _headContainer.RectPivotOffset = new Vector2(headW / 2f, headH);

        _headLeft.RectSize = new Vector2(headH, headH);
        _headLeft.RectPosition = new Vector2(0f, 0f);
        _headLeft.Modulate = new Color(1f, 1f, 1f, Opacity);

        _headCenter.RectSize = new Vector2(headCW, headH);
        _headCenter.RectPosition = new Vector2(headH, 0f);
        _headCenter.Modulate = new Color(1f, 1f, 1f, Opacity);

        _headRight.RectSize = new Vector2(headH, headH);
        _headRight.RectPosition = new Vector2(headW - headH, 0f);
        _headRight.Modulate = new Color(1f, 1f, 1f, Opacity);

        _headOverlay.RectSize = new Vector2(headH, headH);
        _headOverlay.RectPosition = new Vector2(headW / 2f - headH / 2f, 0f);
        _headOverlay.Modulate = new Color(1f, 1f, 1f, Opacity);
      }

      if (bodyDirty)
      {
        // Update body component layout (for Hold notes)
        float bodyW = headW * BodyToHeadRatio;
        float bodyCW = bodyW - headH * 2f;

        _bodyContainer.RectSize = new Vector2(bodyW, BodyHeight);
        _bodyContainer.RectPosition = new Vector2(-bodyW / 2f, -BodyHeight - headH);
        _bodyContainer.RectPivotOffset = new Vector2(bodyW / 2f, BodyHeight);

        _bodyLeft.RectSize = new Vector2(headH, BodyHeight);
        _bodyLeft.RectPosition = new Vector2(0f, 0f);
        _bodyLeft.Modulate = new Color(1f, 1f, 1f, Opacity);

        _bodyCenter.RectSize = new Vector2(bodyCW, BodyHeight);
        _bodyCenter.RectPosition = new Vector2(headH, 0f);
        _bodyCenter.Modulate = new Color(1f, 1f, 1f, Opacity);

        _bodyRight.RectSize = new Vector2(headH, BodyHeight);
        _bodyRight.RectPosition = new Vector2(bodyW - headH, 0f);
        _bodyRight.Modulate = new Color(1f, 1f, 1f, Opacity);
      }

      // Save current state for next dirty check
      _lastState.PlayerAreaSize = PlayerAreaSize;
      _lastState.LaneWidth = LaneWidth;
      _lastState.NoteSize = NoteSize;
      _lastState.BodyHeight = BodyHeight;
      _lastState.Opacity = Opacity;
    }
  }
}