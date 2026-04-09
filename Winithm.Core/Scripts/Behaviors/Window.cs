using Godot;
using Winithm.Core.Constants;
using Winithm.Core.Interfaces;
namespace Winithm.Core.Behaviors
{
  public class Window : Control, IPoolable
  {
    // --- Dirty tracking ---
    struct WindowState
    {
      public Vector2 Pivot, ScreenSize, PlayerAreaSize, WindowSize;
      public Color TitleBarColor, TitleTextColor, WindowColor;
      public string Title;
      public bool Borderless, IsNotRespondingTitle;
      public float UnFocusOverlayOpacity, UnresponsiveOverlayOpacity, NoteOpacity;
    }
    private WindowState _lastState = new WindowState();

    // --- Exported properties ---
    [Export] public Vector2 Pivot = new Vector2(0.5f, 0.5f);
    [Export] public Color TitleBarColor = Colors.Coral;
    [Export] public Color TitleTextColor = Colors.Black;
    [Export] public Vector2 ScreenSize = new Vector2(1280, 720);
    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);

    [Export] public string Title = "Winithm";
    [Export] public Vector2 WindowSize = new Vector2(300, 500);
    [Export] public Color WindowColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    [Export] public float NoteOpacity = 1f;
    [Export] public bool Borderless = false;
    [Export] public bool UnFocus = false;

    // --- Runtime state injected by WindowManager each frame ---
    public float UnFocusOverlayOpacity = 0f;
    public float UnresponsiveOverlayOpacity = 0f;
    public bool IsNotRespondingTitle = false;

    // --- Child references ---
    private Control _titleBar;
    private Control _windowBody;
    private Control _windowFrame;

    // public Vector2 WindowBodySize => _windowBody?.RectSize ?? Vector2.Zero;

    // --- Runtime layers (Z-ordered inside WindowBody) ---
    // NoteLayer → UnfocusOverlay → FocusNoteLayer → UnresponsiveOverlay
    public Control NoteLayer;
    public Control UnfocusOverlay;
    public Control FocusNoteLayer;
    public Control UnresponsiveOverlay;

    // --- Resources ---
    private Texture _iconTex;
    private Texture _closeTex;
    private Texture _maxTex;
    private Texture _minTex;
    private DynamicFont _font;

    public static readonly float TITLE_BAR_HEIGHT_RATIO = 0.0375f;
    public static readonly float UNFOCUS_OVERLAY_TINT = 0.25f;
    internal float TitleBarHeight { get; private set; }

    public override void _Ready()
    {
      _titleBar = GetNode<Control>("TitleBar");
      _windowBody = GetNode<Control>("WindowBody");
      _windowFrame = GetNode<Control>("Frame");

      NoteLayer = CreateLayer("NoteLayer");
      UnfocusOverlay = CreateLayer("UnfocusOverlay");
      FocusNoteLayer = CreateLayer("FocusNoteLayer");
      UnresponsiveOverlay = CreateLayer("UnresponsiveOverlay");

      _titleBar.Connect("draw", this, nameof(OnTitleBarDraw));
      _windowBody.Connect("draw", this, nameof(OnWindowBodyDraw));
      UnfocusOverlay.Connect("draw", this, nameof(OnUnfocusOverlayDraw));
      UnresponsiveOverlay.Connect("draw", this, nameof(OnUnresponsiveOverlayDraw));

      _iconTex = GD.Load<Texture>("res://icon.png");
      _closeTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/WIndow/close.svg");
      _maxTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/WIndow/maximize.svg");
      _minTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/WIndow/minimize.svg");

      var fontData = GD.Load<DynamicFontData>("res://Winithm.Core/Resources/Fonts/Quicksand-Regular.ttf");
      _font = new DynamicFont { FontData = fontData, UseFilter = true };

      UpdateVisual();
    }

    public void OnSpawn() { }
    public void OnDespawn() { }

    public override void _Process(float delta)
    {
      bool overlayDirty =
        UnFocusOverlayOpacity != _lastState.UnFocusOverlayOpacity ||
        UnresponsiveOverlayOpacity != _lastState.UnresponsiveOverlayOpacity;

      if (overlayDirty)
      {
        UnfocusOverlay?.Update();
        UnresponsiveOverlay?.Update();
        _titleBar?.Update();

        _lastState.UnFocusOverlayOpacity = UnFocusOverlayOpacity;
        _lastState.UnresponsiveOverlayOpacity = UnresponsiveOverlayOpacity;
      }
    }

    /// <summary>
    /// Recalculates layout of TitleBar, WindowBody, and Frame.
    /// Call after changing any exported property.
    /// </summary>
    public void UpdateVisual()
    {
      if (_titleBar == null || _windowBody == null) return;

      bool layoutDirty =
        Pivot != _lastState.Pivot ||
        ScreenSize != _lastState.ScreenSize ||
        PlayerAreaSize != _lastState.PlayerAreaSize ||
        WindowSize != _lastState.WindowSize ||
        Borderless != _lastState.Borderless;

      bool titleBarDirty = layoutDirty ||
        TitleBarColor != _lastState.TitleBarColor ||
        TitleTextColor != _lastState.TitleTextColor ||
        Title != _lastState.Title ||
        IsNotRespondingTitle != _lastState.IsNotRespondingTitle;

      bool bodyDirty = layoutDirty ||
        WindowColor != _lastState.WindowColor ||
        NoteOpacity != _lastState.NoteOpacity;

      if (!layoutDirty && !titleBarDirty && !bodyDirty) return;

      if (layoutDirty)
      {
        float viewScale = Mathf.Abs(Mathf.Min(
          PlayerAreaSize.x / Visual.DESIGN_RESOLUTION.x,
          PlayerAreaSize.y / Visual.DESIGN_RESOLUTION.y
        ));

        Vector2 scaledSize = WindowSize * viewScale;
        TitleBarHeight = Mathf.Min(ScreenSize.x, ScreenSize.y) * TITLE_BAR_HEIGHT_RATIO;

        float totalHeight = scaledSize.y + (!Borderless ? TitleBarHeight : 0f);
        Vector2 bodyOffset = new Vector2(
          -scaledSize.x * Pivot.x,
          -totalHeight * Pivot.y + (!Borderless ? TitleBarHeight : 0f)
        );

        _windowBody.RectSize = scaledSize;
        _windowBody.RectPosition = bodyOffset;

        _titleBar.Visible = !Borderless;
        _titleBar.RectSize = new Vector2(scaledSize.x, TitleBarHeight);
        _titleBar.RectPosition = bodyOffset - new Vector2(0f, TitleBarHeight);

        if (_windowFrame != null)
        {
          _windowFrame.Visible = !Borderless;
          _windowFrame.RectSize = new Vector2(scaledSize.x, scaledSize.y + TitleBarHeight);
          _windowFrame.RectPosition = _titleBar.RectPosition;
          _windowFrame.Update();
        }

        _lastState.Pivot = Pivot;
        _lastState.ScreenSize = ScreenSize;
        _lastState.PlayerAreaSize = PlayerAreaSize;
        _lastState.WindowSize = WindowSize;
        _lastState.Borderless = Borderless;
      }

      if (titleBarDirty)
      {
        _titleBar.Update();

        _lastState.TitleBarColor = TitleBarColor;
        _lastState.TitleTextColor = TitleTextColor;
        _lastState.Title = Title;
        _lastState.IsNotRespondingTitle = IsNotRespondingTitle;
      }

      if (bodyDirty)
      {
        _windowBody.Update();

        // Apply NoteOpacity to layers containing notes
        Color noteModulate = new Color(1f, 1f, 1f, NoteOpacity);
        if (NoteLayer != null) NoteLayer.Modulate = noteModulate;
        if (FocusNoteLayer != null) FocusNoteLayer.Modulate = noteModulate;

        _lastState.WindowColor = WindowColor;
        _lastState.NoteOpacity = NoteOpacity;
      }
    }

    // --- Draw callbacks ---

    private void OnTitleBarDraw()
    {
      if (Borderless) return;

      float w = _titleBar.RectSize.x;
      float h = _titleBar.RectSize.y;

      _titleBar.DrawRect(new Rect2(Vector2.Zero, _titleBar.RectSize), TitleBarColor);

      float margin = h * 0.2f;
      float btnSize = h * 0.6f;
      float spacing = h * 1.25f;
      float iconSize = h * 0.7f;

      float iconWidth = margin + iconSize + margin;
      float oneBtn = margin + btnSize + margin;
      float twoBtns = margin + btnSize * 2f + spacing + margin;
      float threeBtns = margin + btnSize * 3f + spacing * 2f + margin;

      bool showIcon = w >= iconWidth;
      bool showClose = w >= iconWidth + oneBtn;
      bool showMax = w >= iconWidth + twoBtns;
      bool showMin = w >= iconWidth + threeBtns;

      int fontSize = (int)(h * 0.55f);
      if (_font.Size != fontSize) _font.Size = fontSize;

      string titleText =
        IsNotRespondingTitle ? Title + " (Not Responding)" : Title;
      string displayTitle = "";
      if (showClose)
      {
        float avail = w - iconWidth - threeBtns - 10f;

        if (_font.GetStringSize(titleText).x <= avail)
        {
          displayTitle = titleText;
        }
        else
        {
          for (int i = titleText.Length - 1; i >= 1; i--)
          {
            string candidate = titleText.Substring(0, i) + "...";
            if (_font.GetStringSize(candidate).x <= avail)
            {
              displayTitle = candidate;
              break;
            }
          }
        }
      }

      float currentX = margin;
      if (showIcon && _iconTex != null)
      {
        _titleBar.DrawTextureRect(
          _iconTex,
          new Rect2(margin, (h - iconSize) / 2f, iconSize, iconSize),
          false
        );
        currentX += iconSize + margin;
      }

      if (!string.IsNullOrEmpty(displayTitle))
      {
        Vector2 textPos = new Vector2(
          currentX,
          h / 2f + _font.GetAscent() / 2f - 2f * (h / 27f)
        );
        _titleBar.DrawString(_font, textPos, displayTitle, TitleTextColor);
      }

      float btnX = w - margin - btnSize;
      float btnY = (h - btnSize) / 2f;

      if (showClose && _closeTex != null)
      {
        _titleBar.DrawTextureRect(_closeTex, new Rect2(btnX, btnY, btnSize, btnSize), false, TitleTextColor);
        btnX -= btnSize + spacing;
      }
      if (showMax && _maxTex != null)
      {
        _titleBar.DrawTextureRect(_maxTex, new Rect2(btnX, btnY, btnSize, btnSize), false, TitleTextColor);
        btnX -= btnSize + spacing;
      }
      if (showMin && _minTex != null)
      {
        _titleBar.DrawTextureRect(_minTex, new Rect2(btnX, btnY, btnSize, btnSize), false, TitleTextColor);
      }

      if (UnresponsiveOverlayOpacity > 0f)
      {
        _titleBar.DrawRect(new Rect2(Vector2.Zero, _titleBar.RectSize), new Color(1f, 1f, 1f, UnresponsiveOverlayOpacity));
      }
    }

    private void OnWindowBodyDraw()
    {
      // Background only — notes and overlays live in Z-ordered layers above
      _windowBody.DrawRect(new Rect2(Vector2.Zero, _windowBody.RectSize), WindowColor);
    }

    private void OnUnfocusOverlayDraw()
    {
      UnfocusOverlay.DrawRect(
        new Rect2(Vector2.Zero, UnfocusOverlay.RectSize),
        new Color(
          UNFOCUS_OVERLAY_TINT,
          UNFOCUS_OVERLAY_TINT,
          UNFOCUS_OVERLAY_TINT,
          UnFocusOverlayOpacity
        )
      );
    }

    private void OnUnresponsiveOverlayDraw()
    {
      UnresponsiveOverlay.DrawRect(
        new Rect2(Vector2.Zero, UnresponsiveOverlay.RectSize),
        new Color(1f, 1f, 1f, UnresponsiveOverlayOpacity)
      );
    }

    // --- Helpers ---

    private Control CreateLayer(string name)
    {
      var layer = new Control
      {
        Name = name,
        MouseFilter = MouseFilterEnum.Ignore
      };
      layer.SetAnchorsAndMarginsPreset(LayoutPreset.Wide);
      _windowBody.AddChild(layer);
      return layer;
    }
  }
}