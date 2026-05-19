using Godot;
using Winithm.Core.Behaviors;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.ResourcePacks.Default.VFX
{
  public class HitFXSC : HitFX
  {
    public float BaseOpacity = 0.5f;
    public float OutlineScale = 0f;
    public float OutlineAlpha = 1f;
    public float FillScale = 0f;
    public float FillAlpha = 1f;
    public float OutlineThickness = 0.05f;

    public float FXWidth = 0.75f;

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

      SetDuration(0.5f);

      Scale = Vector2.One * NoteWidth * FXWidth;

      foreach (Node child in GetChildren())
      {
        if (child is CanvasItem canvasItem)
        {
          canvasItem.Modulate = HitColor;
        }
      }

      OutlineScale = 0f;
      OutlineAlpha = 0.75f;
      FillScale = 0f;
      FillAlpha = 0.75f;
      Update();
    }

    protected override void OnHitFXProcess(float delta)
    {
      float tOutlineScale = Mathf.Clamp(Elapsed / 0.25f, 0f, 1f);
      OutlineScale = (float)EasingFunctions.Evaluate(EasingType.CubicOut, tOutlineScale);

      float tOutlineAlpha = Mathf.Clamp((Elapsed - 0.25f) / 0.1f, 0f, 1f);
      OutlineAlpha = 0.75f - 0.75f * (float)EasingFunctions.Evaluate(EasingType.CubicIn, tOutlineAlpha);

      float tFillScale = Mathf.Clamp(Elapsed / 0.35f, 0f, 1f);
      FillScale = 0.65f * (float)EasingFunctions.Evaluate(EasingType.CubicOut, tFillScale);

      float tFillAlpha = Mathf.Clamp((Elapsed - 0.35f) / 0.15f, 0f, 1f);
      FillAlpha = 0.75f - 0.75f * (float)EasingFunctions.Evaluate(EasingType.CubicIn, tFillAlpha);

      Update();
    }

    protected override void OnHitFXStopped()
    {
      OutlineScale = 0f;
      OutlineAlpha = 0f;
      FillScale = 0f;
      FillAlpha = 0f;
      Update();
    }

    public override void _Draw()
    {
      DrawFilledDiamond();
      DrawOutlineDiamond();
    }

    private void DrawFilledDiamond()
    {
      if (FillScale <= 0f || FillAlpha <= 0f) return;

      float half = 0.5f * FillScale;
      var points = new[]
      {
        new Vector2(0f, -half),
        new Vector2(half, 0f),
        new Vector2(0f, half),
        new Vector2(-half, 0f),
      };

      Color color = HitColor;
      color.a *= BaseOpacity * FillAlpha;
      DrawPolygon(points, new[] { color, color, color, color });
    }

    private void DrawOutlineDiamond()
    {
      if (OutlineScale <= 0f || OutlineAlpha <= 0f || OutlineThickness <= 0f) return;

      float halfOuter = 0.5f * OutlineScale;
      // Perpendicular thickness in local coordinates (percent of scale)
      float localThickness = OutlineThickness;
      float halfInner = Mathf.Max(0f, halfOuter - localThickness * Mathf.Sqrt(2f));

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
      color.a *= BaseOpacity * OutlineAlpha;

      // Draw the hollow diamond as 4 non-overlapping, perfectly mitered trapezoids
      DrawPolygon(new[] { outer[0], outer[1], inner[1], inner[0] }, new[] { color, color, color, color });
      DrawPolygon(new[] { outer[1], outer[2], inner[2], inner[1] }, new[] { color, color, color, color });
      DrawPolygon(new[] { outer[2], outer[3], inner[3], inner[2] }, new[] { color, color, color, color });
      DrawPolygon(new[] { outer[3], outer[0], inner[0], inner[3] }, new[] { color, color, color, color });
    }
  }
}
