using Godot;
using Winithm.Core.Behaviors;

namespace Winithm.Core.ResourcePacks.Default.VFX
{
  public class HitFXSC : HitFX
  {
    [Export] public float BaseOpacity = 0.5f;
    [Export] public float OutlineScale = 0f;
    [Export] public float OutlineAlpha = 1f;
    [Export] public float FillScale = 0f;
    [Export] public float FillAlpha = 1f;
    [Export] public float OutlineThickness = 1f;

    protected override void OnHitFXStarted()
    {
      Update();
    }

    protected override void OnHitFXProcess(float delta)
    {
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
      if (OutlineScale <= 0f || OutlineAlpha <= 0f) return;

      float half = 0.5f * OutlineScale;
      var points = new[]
      {
        new Vector2(0f, -half),
        new Vector2(half, 0f),
        new Vector2(0f, half),
        new Vector2(-half, 0f),
      };

      Color color = HitColor;
      color.a *= BaseOpacity * OutlineAlpha;
      float lineWidth = Mathf.Max(0.004f, OutlineThickness);

      DrawLine(points[0], points[1], color, lineWidth, true);
      DrawLine(points[1], points[2], color, lineWidth, true);
      DrawLine(points[2], points[3], color, lineWidth, true);
      DrawLine(points[3], points[0], color, lineWidth, true);
    }
  }
}
