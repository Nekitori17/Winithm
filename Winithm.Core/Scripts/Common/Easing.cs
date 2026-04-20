using System;
using System.Diagnostics;

namespace Winithm.Core.Common
{
  public enum EasingType
  {
    Linear,
    SineIn,
    SineOut,
    SineInOut,
    CubicIn,
    CubicOut,
    CubicInOut,
    QuadIn,
    QuadOut,
    QuadInOut,
    ExpoIn,
    ExpoOut,
    ExpoInOut,
    CircIn,
    CircOut,
    CircInOut,
    BackIn,
    BackOut,
    BackInOut,
    ElasticIn,
    ElasticOut,
    ElasticInOut,
    BounceIn,
    BounceOut,
    BounceInOut,
    Bezier
  }

  public static class EasingFunctions
  {
    private const double PI = Math.PI;
    private const double HALF_PI = PI / 2f;

    public static double Evaluate(EasingType type, double t)
    {
      t = Math.Max(0f, Math.Min(1f, t));

      switch (type)
      {
        case EasingType.Linear:       return t;
        case EasingType.SineIn:       return SineIn(t);
        case EasingType.SineOut:      return SineOut(t);
        case EasingType.SineInOut:    return SineInOut(t);
        case EasingType.CubicIn:      return CubicIn(t);
        case EasingType.CubicOut:     return CubicOut(t);
        case EasingType.CubicInOut:   return CubicInOut(t);
        case EasingType.QuadIn:       return QuadIn(t);
        case EasingType.QuadOut:      return QuadOut(t);
        case EasingType.QuadInOut:    return QuadInOut(t);
        case EasingType.ExpoIn:       return ExpoIn(t);
        case EasingType.ExpoOut:      return ExpoOut(t);
        case EasingType.ExpoInOut:    return ExpoInOut(t);
        case EasingType.CircIn:       return CircIn(t);
        case EasingType.CircOut:      return CircOut(t);
        case EasingType.CircInOut:    return CircInOut(t);
        case EasingType.BackIn:       return BackIn(t);
        case EasingType.BackOut:      return BackOut(t);
        case EasingType.BackInOut:    return BackInOut(t);
        case EasingType.ElasticIn:    return ElasticIn(t);
        case EasingType.ElasticOut:   return ElasticOut(t);
        case EasingType.ElasticInOut: return ElasticInOut(t);
        case EasingType.BounceIn:     return BounceIn(t);
        case EasingType.BounceOut:    return BounceOut(t);
        case EasingType.BounceInOut:  return BounceInOut(t);
        default:                      return t;
      }
    }

    private static double SineIn(double t) => 1f - Math.Cos(t * HALF_PI);
    private static double SineOut(double t) => Math.Sin(t * HALF_PI);
    private static double SineInOut(double t) => -(Math.Cos(PI * t) - 1f) / 2f;
    private static double CubicIn(double t) => t * t * t;
    private static double CubicOut(double t) { double u = 1f - t; return 1f - u * u * u; }
    private static double CubicInOut(double t) => t < 0.5f ? 4f * t * t * t : 1f - Math.Pow(-2f * t + 2f, 3) / 2f;
    private static double QuadIn(double t) => t * t;
    private static double QuadOut(double t) => t * (2f - t);
    private static double QuadInOut(double t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

    private static double ExpoIn(double t) => t == 0f ? 0f : Math.Pow(2f, 10f * (t - 1f));
    private static double ExpoOut(double t) => t == 1f ? 1f : 1f - Math.Pow(2f, -10f * t);
    private static double ExpoInOut(double t)
    {
      if (t == 0f) return 0f;
      if (t == 1f) return 1f;
      return t < 0.5f
          ? Math.Pow(2f, 20f * t - 10f) / 2f
          : (2f - Math.Pow(2f, -20f * t + 10f)) / 2f;
    }

    private static double CircIn(double t) => 1f - Math.Sqrt(1f - t * t);
    private static double CircOut(double t) { double u = t - 1f; return Math.Sqrt(1f - u * u); }
    private static double CircInOut(double t) => t < 0.5f
            ? (1f - Math.Sqrt(1f - 4f * t * t)) / 2f
            : (Math.Sqrt(1f - Math.Pow(-2f * t + 2f, 2)) + 1f) / 2f;

    private static double BackIn(double t) { const double c1 = 1.70158f; return (c1 + 1f) * t * t * t - c1 * t * t; }
    private static double BackOut(double t) { const double c1 = 1.70158f; double u = t - 1f; return 1f + (c1 + 1f) * u * u * u + c1 * u * u; }
    private static double BackInOut(double t)
    {
      const double c2 = 1.70158f * 1.525f;
      return t < 0.5f
          ? Math.Pow(2f * t, 2) * ((c2 + 1f) * 2f * t - c2) / 2f
          : (Math.Pow(2f * t - 2f, 2) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
    }

    private static double ElasticIn(double t)
    {
      if (t == 0f) return 0f;
      if (t == 1f) return 1f;
      return -Math.Pow(2f, 10f * t - 10f) * Math.Sin((t * 10f - 10.75f) * (2f * PI / 3f));
    }

    private static double ElasticOut(double t)
    {
      if (t == 0f) return 0f;
      if (t == 1f) return 1f;
      return Math.Pow(2f, -10f * t) * Math.Sin((t * 10f - 0.75f) * (2f * PI / 3f)) + 1f;
    }

    private static double ElasticInOut(double t)
    {
      if (t == 0f) return 0f;
      if (t == 1f) return 1f;
      const double c5 = 2f * PI / 4.5f;
      return t < 0.5f
        ? -(Math.Pow(2f, 20f * t - 10f) * Math.Sin((20f * t - 11.125f) * c5)) / 2f
        : Math.Pow(2f, -20f * t + 10f) * Math.Sin((20f * t - 11.125f) * c5) / 2f + 1f;
    }

    private static double BounceIn(double t) => 1f - BounceOut(1f - t);

    private static double BounceOut(double t)
    {
      const double n1 = 7.5625f;
      const double d1 = 2.75f;

      if (t < 1f / d1) return n1 * t * t;
      if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
      if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
      t -= 2.625f / d1;
      return n1 * t * t + 0.984375f;
    }

    private static double BounceInOut(double t)
    {
      return t < 0.5f
        ? (1f - BounceOut(1f - 2f * t)) / 2f
        : (1f + BounceOut(2f * t - 1f)) / 2f;
    }

    public static double EvaluateBezier(AnyValue bezier, double t)
    {
      if (t <= 0f) return 0f;
      if (t >= 1f) return 1f;

      double p1x = Math.Max(0f, Math.Min(1f, bezier.X));
      double p1y = bezier.Y;
      double p2x = Math.Max(0f, Math.Min(1f, bezier.Z));
      double p2y = bezier.W;

      // Binary search to find u parameter where X(u) is close to target t
      double u = t;
      double minU = 0f;
      double maxU = 1f;

      for (int i = 0; i < 12; i++)
      {
        double ou = 1f - u;
        double x = 3f * ou * ou * u * p1x + 3f * ou * u * u * p2x + u * u * u;

        if (Math.Abs(x - t) < 0.0005f) break;

        if (x < t) minU = u;
        else maxU = u;

        u = (minU + maxU) / 2f;
      }

      double finalOu = 1f - u;
      return 3f * finalOu * finalOu * u * p1y + 3f * finalOu * u * u * p2y + u * u * u;
    }

    public static EasingType ParseEasing(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return EasingType.Linear;

      switch (text.Trim())
      {
        case "Linear": return EasingType.Linear;
        case "SineIn": return EasingType.SineIn;
        case "SineOut": return EasingType.SineOut;
        case "SineInOut": return EasingType.SineInOut;
        case "CubicIn": return EasingType.CubicIn;
        case "CubicOut": return EasingType.CubicOut;
        case "CubicInOut": return EasingType.CubicInOut;
        case "QuadIn": return EasingType.QuadIn;
        case "QuadOut": return EasingType.QuadOut;
        case "QuadInOut": return EasingType.QuadInOut;
        case "ExpoIn": return EasingType.ExpoIn;
        case "ExpoOut": return EasingType.ExpoOut;
        case "ExpoInOut": return EasingType.ExpoInOut;
        case "CircIn": return EasingType.CircIn;
        case "CircOut": return EasingType.CircOut;
        case "CircInOut": return EasingType.CircInOut;
        case "BackIn": return EasingType.BackIn;
        case "BackOut": return EasingType.BackOut;
        case "BackInOut": return EasingType.BackInOut;
        case "ElasticIn": return EasingType.ElasticIn;
        case "ElasticOut": return EasingType.ElasticOut;
        case "ElasticInOut": return EasingType.ElasticInOut;
        case "BounceIn": return EasingType.BounceIn;
        case "BounceOut": return EasingType.BounceOut;
        case "BounceInOut": return EasingType.BounceInOut;
        // Alias support
        case "EaseIn": return EasingType.CubicIn;
        case "EaseOut": return EasingType.CubicOut;
        case "EaseInOut": return EasingType.CubicInOut;
        default:
          Trace.TraceWarning($"[WinithmParser] Unknown easing type: '{text}', falling back to Linear.");
          return EasingType.Linear;
      }
    }
  }
}
