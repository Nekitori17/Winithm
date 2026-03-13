using System;

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
        private const float PI = (float)Math.PI;
        private const float HALF_PI = PI / 2f;

        public static float Evaluate(EasingType type, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));

            switch (type)
            {
                case EasingType.Linear:      return t;
                case EasingType.SineIn:      return SineIn(t);
                case EasingType.SineOut:     return SineOut(t);
                case EasingType.SineInOut:   return SineInOut(t);
                case EasingType.CubicIn:     return CubicIn(t);
                case EasingType.CubicOut:    return CubicOut(t);
                case EasingType.CubicInOut:  return CubicInOut(t);
                case EasingType.QuadIn:      return QuadIn(t);
                case EasingType.QuadOut:     return QuadOut(t);
                case EasingType.QuadInOut:   return QuadInOut(t);
                case EasingType.ExpoIn:      return ExpoIn(t);
                case EasingType.ExpoOut:     return ExpoOut(t);
                case EasingType.ExpoInOut:   return ExpoInOut(t);
                case EasingType.CircIn:      return CircIn(t);
                case EasingType.CircOut:     return CircOut(t);
                case EasingType.CircInOut:   return CircInOut(t);
                case EasingType.BackIn:      return BackIn(t);
                case EasingType.BackOut:     return BackOut(t);
                case EasingType.BackInOut:   return BackInOut(t);
                case EasingType.ElasticIn:   return ElasticIn(t);
                case EasingType.ElasticOut:  return ElasticOut(t);
                case EasingType.ElasticInOut: return ElasticInOut(t);
                case EasingType.BounceIn:    return BounceIn(t);
                case EasingType.BounceOut:   return BounceOut(t);
                case EasingType.BounceInOut: return BounceInOut(t);
                default: return t;
            }
        }

        private static float SineIn(float t) => 1f - (float)Math.Cos(t * HALF_PI);
        private static float SineOut(float t) => (float)Math.Sin(t * HALF_PI);
        private static float SineInOut(float t) => -(float)(Math.Cos(PI * t) - 1f) / 2f;
        private static float CubicIn(float t) => t * t * t;
        private static float CubicOut(float t) { float u = 1f - t; return 1f - u * u * u; }
        private static float CubicInOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;
        private static float QuadIn(float t) => t * t;
        private static float QuadOut(float t) => t * (2f - t);
        private static float QuadInOut(float t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        
        private static float ExpoIn(float t) => t == 0f ? 0f : (float)Math.Pow(2f, 10f * (t - 1f));
        private static float ExpoOut(float t) => t == 1f ? 1f : 1f - (float)Math.Pow(2f, -10f * t);
        private static float ExpoInOut(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return t < 0.5f
                ? (float)Math.Pow(2f, 20f * t - 10f) / 2f
                : (2f - (float)Math.Pow(2f, -20f * t + 10f)) / 2f;
        }

        private static float CircIn(float t) => 1f - (float)Math.Sqrt(1f - t * t);
        private static float CircOut(float t) { float u = t - 1f; return (float)Math.Sqrt(1f - u * u); }
        private static float CircInOut(float t) => t < 0.5f
                ? (1f - (float)Math.Sqrt(1f - 4f * t * t)) / 2f
                : ((float)Math.Sqrt(1f - (float)Math.Pow(-2f * t + 2f, 2)) + 1f) / 2f;

        private static float BackIn(float t) { const float c1 = 1.70158f; return (c1 + 1f) * t * t * t - c1 * t * t; }
        private static float BackOut(float t) { const float c1 = 1.70158f; float u = t - 1f; return 1f + (c1 + 1f) * u * u * u + c1 * u * u; }
        private static float BackInOut(float t)
        {
            const float c2 = 1.70158f * 1.525f;
            return t < 0.5f
                ? (float)(Math.Pow(2f * t, 2) * ((c2 + 1f) * 2f * t - c2)) / 2f
                : (float)(Math.Pow(2f * t - 2f, 2) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
        }

        private static float ElasticIn(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return -(float)Math.Pow(2f, 10f * t - 10f) * (float)Math.Sin((t * 10f - 10.75f) * (2f * PI / 3f));
        }

        private static float ElasticOut(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return (float)Math.Pow(2f, -10f * t) * (float)Math.Sin((t * 10f - 0.75f) * (2f * PI / 3f)) + 1f;
        }

        private static float ElasticInOut(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            const float c5 = (2f * PI) / 4.5f;
            return t < 0.5f
              ? -(float)(Math.Pow(2f, 20f * t - 10f) * Math.Sin((20f * t - 11.125f) * c5)) / 2f
              : (float)(Math.Pow(2f, -20f * t + 10f) * Math.Sin((20f * t - 11.125f) * c5)) / 2f + 1f;
        }

        private static float BounceIn(float t) => 1f - BounceOut(1f - t);

        private static float BounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        private static float BounceInOut(float t)
        {
            return t < 0.5f
              ? (1f - BounceOut(1f - 2f * t)) / 2f
              : (1f + BounceOut(2f * t - 1f)) / 2f;
        }

        public static float EvaluateBezier(VectorValue bezier, float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            float p1x = Math.Max(0f, Math.Min(1f, bezier.X));
            float p1y = bezier.Y;
            float p2x = Math.Max(0f, Math.Min(1f, bezier.Z));
            float p2y = bezier.W;

            // Binary search to find u parameter where X(u) is close to target t
            float u = t; 
            float minU = 0f;
            float maxU = 1f;
            
            for (int i = 0; i < 12; i++) 
            {
                float ou = 1f - u;
                float x = 3f * ou * ou * u * p1x + 3f * ou * u * u * p2x + u * u * u;
                
                if (Math.Abs(x - t) < 0.0005f) break;

                if (x < t) minU = u;
                else maxU = u;

                u = (minU + maxU) / 2f;
            }

            float finalOu = 1f - u;
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
                    Godot.GD.PushWarning($"[WinithmParser] Unknown easing type: '{text}', falling back to Linear.");
                    return EasingType.Linear;
            }
        }
    }
}
