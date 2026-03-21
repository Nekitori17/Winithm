using Godot;
using System;

namespace Winithm.Core.Behaviors
{
  public class Window : Control
  {
    [Export] public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);
    [Export] public Color TitleBarColor { get; set; } = Colors.Aqua;
    [Export] public Vector2 ScreenSize { get; set; } = new Vector2(1280, 720);
    [Export] public Vector2 GameScreenSize { get; set; } = new Vector2(1280, 720);

    [Export] public string Title { get; set; } = "Winithm";
    [Export] public Vector2 WindowSize { get; set; } = new Vector2(300, 500);
    [Export] public Color WindowColor { get; set; } = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    [Export] public bool Borderless { get; set; } = false;
    [Export] public bool Unfocus { get; set; } = false;
    [Export] public bool Focusable { get; set; } = false;

    // --- Fields ---
    public Control _titleBar;
    public Control _windowBody;
    public Control _body;
    public Control _frame;

    private Texture _iconTex;
    private Texture _closeTex;
    private Texture _maxTex;
    private Texture _minTex;
    private DynamicFont _font;

    private float _focusAnimTime = 0f;

    // Cached title bar height in local pixels (updated in RefreshWindowLayout)
    private float _titleBarHeight = Mathf.Min(1280, 720) * 0.0375f;

    public override void _Ready()
    {
      RectClipContent = false;
      SetProcess(true);
      _titleBar = GetNode<Control>("TitleBar");
      _windowBody = GetNode<Control>("WindowBody");
      _body = GetNode<Control>("WindowBody/Body");
      _frame = GetNode<Control>("Frame");

      // Load Resources
      _iconTex = GD.Load<Texture>("res://icon.png");
      _closeTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/WIndow/close.svg");
      _maxTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/WIndow/maximize.svg");
      _minTex = GD.Load<Texture>("res://Winithm.Core/Resources/Icons/WIndow/minimize.svg");

      var fontData = GD.Load<DynamicFontData>("res://Winithm.Core/Resources/Fonts/Quicksand-Regular.ttf");
      _font = new DynamicFont
      {
        FontData = fontData,
        UseFilter = true
      };

      if (_titleBar != null) _titleBar.Connect("draw", this, nameof(OnTitleBarDraw));
      if (_body != null) _body.Connect("draw", this, nameof(OnBodyDraw));

      UpdateWindow();
    }

    public override void _Process(float delta)
    {
      if (Focusable)
      {
        _focusAnimTime += delta * 20f;
        _body?.Update();
        _titleBar?.Update();
      }
    }

    // Design resolution constant — Window was designed at this resolution
    private static readonly Vector2 DesignResolution = new Vector2(1280f, 720f);

    public void UpdateWindow()
    {
      if (_titleBar == null || _windowBody == null) return;

      _titleBar.Visible = !Borderless;
      if (_frame != null) _frame.Visible = !Borderless;

      // ViewScale: how much the current screen differs from the design resolution
      float viewScale = Mathf.Min(
        GameScreenSize.x / DesignResolution.x,
        GameScreenSize.y / DesignResolution.y
      );
      viewScale = Mathf.Abs(viewScale); // Prevents negative values

      // Scale WindowSize and title bar height by viewScale
      Vector2 scaledSize = WindowSize * viewScale;
      _titleBarHeight = Mathf.Min(ScreenSize.x, ScreenSize.y) * 0.0375f;

      Vector2 bodyOffset = -scaledSize * Pivot;
      _windowBody.RectSize = scaledSize;

      if (!Borderless)
      {
        _titleBar.Visible = true;
        _titleBar.RectSize = new Vector2(scaledSize.x, _titleBarHeight);
        bodyOffset += new Vector2(0, _titleBarHeight);
      }
      else
      {
        _titleBar.Visible = false;
      }

      _titleBar.RectPosition = bodyOffset - new Vector2(0, _titleBarHeight);
      _windowBody.RectPosition = bodyOffset;

      if (_frame != null)
      {
        if (!Borderless)
        {
          _frame.Visible = true;
          _frame.RectSize = new Vector2(scaledSize.x, scaledSize.y + _titleBarHeight);
          _frame.RectPosition = _titleBar.RectPosition;
          _frame.Update();
        }
        else
        {
          _frame.Visible = false;
        }
      }

      if (_body != null)
      {
        _body.RectSize = scaledSize;
        _body.RectPosition = Vector2.Zero;
      }
      Update();
      _titleBar.Update();
      _body.Update();
    }

    private void OnTitleBarDraw()
    {
      if (Borderless || _titleBar == null) return;

      float width = _titleBar.RectSize.x;
      float height = _titleBar.RectSize.y;

      // Draw background
      Color bgColor = TitleBarColor;
      _titleBar.DrawRect(new Rect2(Vector2.Zero, _titleBar.RectSize), bgColor);

      float margin = height * 0.2f;
      float btnSize = height * 0.6f;
      float spacing = height * 1.25f;
      float iconSize = height * 0.7f;

      // 1. Core areas needed for each button (Close -> Max -> Min)
      float closeArea = margin + btnSize + margin;
      float maxArea   = closeArea + spacing + btnSize;
      float minArea   = maxArea + spacing + btnSize;

      // 2. Space needed for Icon and Title
      float iconNeeded = margin + iconSize + margin;
      float titleNeeded = height * 1.5f; // Threshold for title to exist

      // 3. PRIORITY HIERARCHY (Icon > Title > Close > Max > Min)
      // Hide buttons if we don't have space for Icon + Title
      float iconTitleSpace = iconNeeded + titleNeeded;
      bool showMin   = width >= (minArea + iconTitleSpace);
      bool showMax   = width >= (maxArea + iconTitleSpace);
      bool showClose = width >= (closeArea + iconTitleSpace);

      // Total space on the right side based on visible buttons
      float buttonsSpace = margin;
      if (showClose) buttonsSpace = closeArea;
      if (showMax)   buttonsSpace = maxArea;
      if (showMin)   buttonsSpace = minArea;

      // 4. Decide Title and Icon visibility (Icon is absolute last)
      bool showTitle = width >= (iconNeeded + (showClose ? 40f : 10f));
      bool shortenTitle = width < (buttonsSpace + iconNeeded + height * 4f);
      bool showIcon = width >= (margin + iconSize);

      float currentX = margin;

      // Draw Icon (Priority: 1)
      if (showIcon && _iconTex != null)
      {
        _titleBar.DrawTextureRect(_iconTex, new Rect2(margin, (height - iconSize) / 2f, iconSize, iconSize), false);
        currentX += iconSize + margin;
      }

      // Draw Title (Priority: 2)
      if (showTitle && _font != null)
      {
        _font.Size = (int)Mathf.Max(height * 0.55f, 8);
        string displayTitle = Title;
        if (shortenTitle && displayTitle.Length > 5)
          displayTitle = displayTitle.Substring(0, 5) + "...";

        Vector2 textPos = new Vector2(currentX, height / 2f + _font.GetAscent() / 2f - 2f * (height / 27f));
        _titleBar.DrawString(_font, textPos, displayTitle, Colors.Black);
      }

      // Draw Buttons (Priority: 3/4/5)
      float btnX = width - margin - btnSize;
      float btnY = (height - btnSize) / 2f;
      Color btnColor = Colors.Black;

      if (showClose && _closeTex != null)
      {
        _titleBar.DrawTextureRect(_closeTex, new Rect2(btnX, btnY, btnSize, btnSize), false, btnColor);
        btnX -= (btnSize + spacing);
      }
      if (showMax && _maxTex != null)
      {
        _titleBar.DrawTextureRect(_maxTex, new Rect2(btnX, btnY, btnSize, btnSize), false, btnColor);
        btnX -= (btnSize + spacing);
      }
      if (showMin && _minTex != null)
      {
        _titleBar.DrawTextureRect(_minTex, new Rect2(btnX, btnY, btnSize, btnSize), false, btnColor);
      }
    }

    private void OnBodyDraw()
    {
      if (_body == null) return;

      // Draw content background
      _body.DrawRect(new Rect2(Vector2.Zero, _body.RectSize), WindowColor);

      // Focusable Flicker / Flash (White background)
      if (Focusable && Mathf.Sin(_focusAnimTime) > 0)
      {
          // Draw pure white over the background
          _body.DrawRect(new Rect2(Vector2.Zero, _body.RectSize), new Color(1, 1, 1, 0.1f));
      }

      // Unfocus overlay
      if (Unfocus)
      {
        _body.DrawRect(new Rect2(Vector2.Zero, _body.RectSize), new Color(0, 0, 0, 0.45f));
      }
    }
  }
}