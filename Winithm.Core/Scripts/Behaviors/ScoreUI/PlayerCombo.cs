using Godot;
using Winithm.Core.Constants;

namespace Winithm.Core.Behaviors.ScoreUI
{
  public class PlayerCombo : Control
  {
    public struct LastState
    {
      public Vector2 ScreenSize;
      public Color TextColor, TextOutLineColor;
    }

    [Export] public Vector2 ScreenSize = Visual.DESIGN_RESOLUTION;
    [Export] public Color TextColor = Colors.White;
    [Export] public Color TextOutLineColor = Colors.Black;

    private LastState _lastState = new LastState();

    private Label _comboLabel;
    private Label _statusLabel;
    private Control _pauseControl;
    private ColorRect _progressRect;

    public enum PauseAnimState { Idle, Draining, Filling }
    private PauseAnimState _pauseState = PauseAnimState.Idle;
    private float _pauseTimer = 0f;
    private const float PAUSE_DURATION = 0.5f;

    private float _comboColorTimer = 0f;
    private const float COMBO_COLOR_DURATION = 0.25f;

    private readonly Vector2 BASE_COMBO_POS = new Vector2(55f, 25f);
    private readonly Vector2 BASE_STATUS_POS = new Vector2(55f, 75f);
    private readonly Vector2 BASE_PAUSE_POS = new Vector2(25f, 25f);

    public override void _Ready()
    {
      _comboLabel = GetNodeOrNull<Label>("Combo");
      _statusLabel = GetNodeOrNull<Label>("Status");
      _pauseControl = GetNodeOrNull<Control>("Pause");
      _progressRect = _pauseControl?.GetNodeOrNull<ColorRect>("Progress");
      UpdateVisual();
    }

    public override void _Process(float delta)
    {
      if (_comboColorTimer > 0f)
      {
        _comboColorTimer -= delta;
        if (_comboColorTimer < 0f) _comboColorTimer = 0f;

        float t = 1f - (_comboColorTimer / COMBO_COLOR_DURATION);
        Color inverted = new Color(1f - TextColor.r, 1f - TextColor.g, 1f - TextColor.b, TextColor.a);
        if (_progressRect != null) _progressRect.Color = inverted.LinearInterpolate(TextColor, t);
      }
      else if (_progressRect != null)
      {
        _progressRect.Color = TextColor;
      }

      if (_pauseState == PauseAnimState.Draining)
      {
        _pauseTimer += delta;
        if (_pauseTimer >= PAUSE_DURATION)
        {
          _pauseTimer = PAUSE_DURATION;
          _pauseState = PauseAnimState.Idle;
        }
      }
      else if (_pauseState == PauseAnimState.Filling)
      {
        _pauseTimer -= delta;
        if (_pauseTimer <= 0f)
        {
          _pauseTimer = 0f;
          _pauseState = PauseAnimState.Idle;
        }
      }

      if (_progressRect != null && _pauseControl != null)
      {
        float t = _pauseTimer / PAUSE_DURATION;
        float yOffset = _pauseControl.RectSize.y * t;
        _progressRect.MarginTop = yOffset;
        _progressRect.MarginBottom = yOffset;
      }
    }

    public void UpdateVisual()
    {
      bool isLayoutDirty = ScreenSize != _lastState.ScreenSize;
      bool isColorDirty =
        TextColor != _lastState.TextColor
        || TextOutLineColor != _lastState.TextOutLineColor;

      if (isLayoutDirty) UpdateLayout();
      if (isColorDirty) UpdateColor();
    }

    private void UpdateLayout()
    {
      float viewScale = Mathf.Abs(Mathf.Min(
        ScreenSize.x / Visual.DESIGN_RESOLUTION.x,
        ScreenSize.y / Visual.DESIGN_RESOLUTION.y
      ));

      if (_comboLabel != null) _comboLabel.RectPosition = BASE_COMBO_POS * viewScale;
      if (_statusLabel != null) _statusLabel.RectPosition = BASE_STATUS_POS * viewScale;
      if (_pauseControl != null)
      {
        _pauseControl.RectPosition = BASE_PAUSE_POS * viewScale;
        _pauseControl.RectSize = new Vector2(15f, 75f) * viewScale;
      }

      _lastState.ScreenSize = ScreenSize;
    }

    private void UpdateColor()
    {
      if (_comboLabel != null)
      {
        _comboLabel.AddColorOverride("font_color", TextColor);
        _comboLabel.AddColorOverride("font_outline_modulate", TextOutLineColor);
      }
      if (_statusLabel != null)
      {
        _statusLabel.AddColorOverride("font_color", TextColor);
        _statusLabel.AddColorOverride("font_outline_modulate", TextOutLineColor);
      }

      if (_progressRect != null && _pauseState == PauseAnimState.Idle)
      {
        _progressRect.Color = TextColor;
      }

      _lastState.TextColor = TextColor;
      _lastState.TextOutLineColor = TextOutLineColor;
    }

    public void SetCombo(int combo, bool instant)
    {
      if (_comboLabel != null) _comboLabel.Text = $"x{combo}";
      if (instant) return;
      _comboColorTimer = COMBO_COLOR_DURATION;
    }

    public void SetGrade(Scoring.Grade grade)
    {
      if (_statusLabel != null) _statusLabel.Text = $"RANK: {Scoring.GradeNames[grade]}";
    }

    public void DrainPauseBar() => _pauseState = PauseAnimState.Draining;

    public void FillPauseBar() => _pauseState = PauseAnimState.Filling;
  }
}
