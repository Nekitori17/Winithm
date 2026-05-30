using System.Drawing;
using Godot;
using Winithm.Core;
using Winithm.Core.Interfaces;
namespace Winithm.Core.Behaviors
{
  public class Window : Node2D, IPoolable
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
    public Control TitleBar { get; private set; }
    public Control WindowBody { get; private set; }
    public Control WindowFrame { get; private set; }

    // public Vector2 WindowBodySize => WindowBody?.RectSize ?? Vector2.Zero;

    // --- Runtime layers (Z-ordered inside WindowBody) ---
    // NoteLayer → UnfocusOverlay → FocusNoteLayer → UnresponsiveOverlay → HitFXLayer
    public Control NoteLayer { get; private set; }
    public Control UnfocusOverlay { get; private set; }
    public Control FocusNoteLayer { get; private set; }
    public Control UnresponsiveOverlay { get; private set; }

    // --- Resources ---
    private static readonly Texture _iconTex = GD.Load<Texture>("res://icon.png");
    private static readonly Texture _closeTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/Window/close.svg");
    private static readonly Texture _maxTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/Window/maximize.svg");
    private static readonly Texture _minTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/Window/minimize.svg");
    private static readonly DynamicFont _font = new DynamicFont
    {
      FontData = GD.Load<DynamicFontData>("res://Winithm.Core/Resources/Fonts/Quicksand.ttf"),
      UseFilter = true
    };

    public static readonly float TITLE_BAR_HEIGHT_RATIO = 0.0375f;
    public static readonly Color UNFOCUS_OVERLAY_TINT = new Color(0.25f, 0.25f, 0.25f, 0.5f);
    public static readonly Color UNRESPONSIVE_OVERLAY_TINT = new Color(1f, 1f, 1f, 0.75f);
    public static readonly Color UNRESPONSIVE_WINDOW_MODULATE = new Color(1f, 1f, 1f, 0.75f);
    internal float TitleBarHeight { get; private set; }

    public override void _Ready()
    {
      TitleBar = GetNode<Control>("TitleBar");
      WindowBody = GetNode<Control>("WindowBody");
      WindowFrame = GetNode<Control>("Frame");

      NoteLayer = GetNode<Control>("WindowBody/NoteLayer");
      UnfocusOverlay = GetNode<Control>("WindowBody/UnfocusOverlay");
      FocusNoteLayer = GetNode<Control>("WindowBody/FocusNoteLayer");
      UnresponsiveOverlay = GetNode<Control>("WindowBody/UnresponsiveOverlay");

      TitleBar.Connect("draw", this, nameof(OnTitleBarDraw));
      WindowBody.Connect("draw", this, nameof(OnWindowBodyDraw));
      UnfocusOverlay.Connect("draw", this, nameof(OnUnfocusOverlayDraw));
      UnresponsiveOverlay.Connect("draw", this, nameof(OnUnresponsiveOverlayDraw));

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
        TitleBar?.Update();

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
      if (TitleBar == null || WindowBody == null) return;

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
          PlayerAreaSize.x / Constants.Visual.DESIGN_RESOLUTION.x,
          PlayerAreaSize.y / Constants.Visual.DESIGN_RESOLUTION.y
        ));

        Vector2 scaledSize = WindowSize * viewScale;
        TitleBarHeight = Mathf.Min(ScreenSize.x, ScreenSize.y) * TITLE_BAR_HEIGHT_RATIO;

        float totalHeight = scaledSize.y + (!Borderless ? TitleBarHeight : 0f);
        Vector2 bodyOffset = new Vector2(
          -scaledSize.x * Pivot.x,
          -totalHeight * Pivot.y + (!Borderless ? TitleBarHeight : 0f)
        );

        WindowBody.RectSize = scaledSize;
        WindowBody.RectPosition = bodyOffset;

        TitleBar.Visible = !Borderless;
        TitleBar.RectSize = new Vector2(scaledSize.x, TitleBarHeight);
        TitleBar.RectPosition = bodyOffset - new Vector2(0f, TitleBarHeight);

        if (WindowFrame != null)
        {
          WindowFrame.Visible = !Borderless;
          WindowFrame.RectSize = new Vector2(scaledSize.x, scaledSize.y + TitleBarHeight);
          WindowFrame.RectPosition = TitleBar.RectPosition;
          WindowFrame.Update();
        }

        _lastState.Pivot = Pivot;
        _lastState.ScreenSize = ScreenSize;
        _lastState.PlayerAreaSize = PlayerAreaSize;
        _lastState.WindowSize = WindowSize;
        _lastState.Borderless = Borderless;
      }

      if (titleBarDirty)
      {
        TitleBar.Update();

        _lastState.TitleBarColor = TitleBarColor;
        _lastState.TitleTextColor = TitleTextColor;
        _lastState.Title = Title;
        _lastState.IsNotRespondingTitle = IsNotRespondingTitle;
      }

      if (bodyDirty)
      {
        WindowBody.Update();

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

      float w = TitleBar.RectSize.x;
      float h = TitleBar.RectSize.y;

      TitleBar.DrawRect(new Rect2(Vector2.Zero, TitleBar.RectSize), TitleBarColor);

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
      bool fontReady = _font?.FontData != null;
      if (fontReady && _font.Size != fontSize) _font.Size = fontSize;

      string titleText =
        IsNotRespondingTitle ? (Title ?? "") + " (Not Responding)" : (Title ?? "");
      string displayTitle = "";
      if (showClose && fontReady && titleText.Length > 0)
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
        TitleBar.DrawTextureRect(
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
        TitleBar.DrawString(_font, textPos, displayTitle, TitleTextColor);
      }

      float btnX = w - margin - btnSize;
      float btnY = (h - btnSize) / 2f;

      if (showClose && _closeTex != null)
      {
        TitleBar.DrawTextureRect(_closeTex, new Rect2(btnX, btnY, btnSize, btnSize), false, TitleTextColor);
        btnX -= btnSize + spacing;
      }
      if (showMax && _maxTex != null)
      {
        TitleBar.DrawTextureRect(_maxTex, new Rect2(btnX, btnY, btnSize, btnSize), false, TitleTextColor);
        btnX -= btnSize + spacing;
      }
      if (showMin && _minTex != null)
      {
        TitleBar.DrawTextureRect(_minTex, new Rect2(btnX, btnY, btnSize, btnSize), false, TitleTextColor);
      }

      if (UnresponsiveOverlayOpacity > 0f)
      {
        TitleBar.DrawRect(new Rect2(Vector2.Zero, TitleBar.RectSize), new Color(1f, 1f, 1f, UnresponsiveOverlayOpacity));
      }
    }

    private void OnWindowBodyDraw()
    { 
      WindowColor.a *= Mathf.Lerp(1f, 0.9f, UnFocusOverlayOpacity);

      // Background only — notes and overlays live in Z-ordered layers above
      WindowBody.DrawRect(new Rect2(Vector2.Zero, WindowBody.RectSize), WindowColor);
    }

    private void OnUnfocusOverlayDraw()
    {
      Color unfocusColor = new Color(UNFOCUS_OVERLAY_TINT)
      {
        a = UnFocusOverlayOpacity
      };

      UnfocusOverlay.DrawRect(
        new Rect2(Vector2.Zero, UnfocusOverlay.RectSize),
        unfocusColor
      );
    }

    private void OnUnresponsiveOverlayDraw()
    {
      Color unresponsiveColor = new Color(UNFOCUS_OVERLAY_TINT)
      {
        a = UnresponsiveOverlayOpacity
      };

      UnresponsiveOverlay.DrawRect(
        new Rect2(Vector2.Zero, UnresponsiveOverlay.RectSize),
        unresponsiveColor
      );
    }
  }
}
