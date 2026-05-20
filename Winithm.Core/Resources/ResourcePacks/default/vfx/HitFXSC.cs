using Godot;
using Winithm.Core.Behaviors;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.ResourcePacks.Default.VFX
{
  public class HitFXSC : HitFX
  {
    public readonly float Duration = 0.5f;
    public readonly float Opacity = 0.35f;
    public readonly float FXWidth = 0.75f;
    public readonly float OutlineThickness = 0.05f;

    private float _outlineScale = 0f;
    private float _outlineAlpha = 1f;
    private float _fillScale = 0f;
    private float _fillAlpha = 1f;
    private float _fillWidth = 1f;


    protected override void OnHitFXStarted()
    {
      switch (ResultType)
      {
        case HitResultType.Perfect: HitColor = Colors.Yellow; break;
        case HitResultType.Good: HitColor = Colors.Cyan; break;
        case HitResultType.Bad: HitColor = new Color(1f, 0f, 0.45f, 1f); break;
        case HitResultType.Miss: HitColor = new Color(0.5f, 0.5f, 0.5f, 1f); break;
        default: HitColor = Colors.Yellow; break;
      }

      SetDuration(Duration);

      Scale = Vector2.One * NoteWidth * FXWidth;

      foreach (Node child in GetChildren())
      {
        if (child is CanvasItem canvasItem)
        {
          canvasItem.Modulate = HitColor;
        }
      }

      _outlineScale = 0f;
      _outlineAlpha = 1f;
      _fillScale = 0f;
      _fillAlpha = 1f;
      _fillWidth = 1f;
      Update();
    }

    protected override void OnHitFXProcess(float delta)
    {
      float tOutlineScale = Mathf.Clamp(Elapsed / 0.25f, 0f, 1f);
      _outlineScale = (float)EasingFunctions.Evaluate(EasingType.CubicOut, tOutlineScale);

      float t_outlineAlpha = Mathf.Clamp((Elapsed - 0.25f) / 0.1f, 0f, 1f);
      _outlineAlpha = 1f - 0.75f * (float)EasingFunctions.Evaluate(EasingType.CubicIn, t_outlineAlpha);

      float t_fillScale = Mathf.Clamp(Elapsed / 0.35f, 0f, 1f);
      _fillScale = 0.65f * (float)EasingFunctions.Evaluate(EasingType.CubicOut, t_fillScale);

      float t_fillAlpha = Mathf.Clamp((Elapsed - 0.35f) / 0.15f, 0f, 1f);
      _fillAlpha = 1f - 0.75f * (float)EasingFunctions.Evaluate(EasingType.CubicIn, t_fillAlpha);

      float t_fillWidth = Mathf.Clamp((Elapsed - 0.35f) / 0.15f, 0f, 1f);
      _fillWidth = 1f - 1f * (float)EasingFunctions.Evaluate(EasingType.CubicOut, t_fillWidth);

      Update();
    }

    protected override void OnHitFXStopped()
    {
      _outlineScale = 0f;
      _outlineAlpha = 0f;
      _fillScale = 0f;
      _fillAlpha = 0f;
      _fillWidth = 0f;
      Update();
    }

    public override void _Draw()
    {
      DrawFilledDiamond();
      DrawOutlineDiamond();
    }

    private void DrawFilledDiamond()
    {
      if (_fillScale <= 0f || _fillAlpha <= 0f) return;

      float currentThickness = Mathf.Lerp(0, 0.5f * _fillScale, _fillWidth);
      DrawHollowDiamond(_fillScale, currentThickness, _fillAlpha);
    }

    private void DrawOutlineDiamond()
    {
      if (_outlineScale <= 0f || _outlineAlpha <= 0f || OutlineThickness <= 0f) return;

      DrawHollowDiamond(_outlineScale, OutlineThickness, _outlineAlpha);
    }

    private void DrawHollowDiamond(float scale, float thickness, float alpha)
    {
      float halfOuter = 0.5f * scale;
      float halfInner = Mathf.Max(0f, halfOuter - thickness);

      var outer = new[]
      {
        new Vector2(0f, -halfOuter),
        new Vector2(halfOuter, 0f),
        new Vector2(0f, halfOuter),
        new Vector2(-halfOuter, 0f)
      };

      var inner = new[]
      {
        new Vector2(0f, -halfInner),
        new Vector2(halfInner, 0f),
        new Vector2(0f, halfInner),
        new Vector2(-halfInner, 0f)
      };

      Color color = HitColor;
      color.a *= Opacity * alpha;

      // Draw the hollow diamond as 4 non-overlapping, perfectly mitered trapezoids
      DrawPolygon(new[] { outer[0], outer[1], inner[1], inner[0] }, new[] { color, color, color, color });
      DrawPolygon(new[] { outer[1], outer[2], inner[2], inner[1] }, new[] { color, color, color, color });
      DrawPolygon(new[] { outer[2], outer[3], inner[3], inner[2] }, new[] { color, color, color, color });
      DrawPolygon(new[] { outer[3], outer[0], inner[0], inner[3] }, new[] { color, color, color, color });
    }
  }
}
