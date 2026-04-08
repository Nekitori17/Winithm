namespace Winithm.Core.Common
{
  /// <summary>
  /// Deterministic timing format: B:N/D
  /// where B = base beat, N = numerator, D = denominator.
  /// Example: "0:1/2" = 0.5 beats, "1:3/4" = 1.75 beats
  /// </summary>
  public class BeatTime
  {
    public int Beat;
    public int Numerator;
    public int Denominator;

    /// <summary>Pre-computed absolute beat value (e.g., 0:1/2 → 0.5f)</summary>
    public float AbsoluteValue;

    public BeatTime(int beat, int numerator, int denominator)
    {
      Beat = beat;
      Numerator = numerator;
      Denominator = denominator;

      if (numerator != 0 && denominator == 0)
      {
        System.Diagnostics.Trace.TraceWarning($"[BeatTime] Numerator={numerator} but Denominator=0, fractional beat component discarded.");
        AbsoluteValue = beat;
      }
      else
      {
        AbsoluteValue = denominator != 0 ? beat + (float)numerator / denominator : beat;
      }
    }

    /// <summary>
    /// Parse a BeatTime string in the format "B:N/D".
    /// </summary>
    public static BeatTime Parse(string text)
    {
      text = text.Trim();

      int colonIndex = text.IndexOf(':');
      if (colonIndex < 0)
        return new BeatTime(int.Parse(text), 0, 0);

      int slashIndex = text.IndexOf('/');
      if (slashIndex < 0)
        return new BeatTime(int.Parse(text.Substring(0, colonIndex)), 0, 0);

      int beat = int.Parse(text.Substring(0, colonIndex));
      int numerator = int.Parse(text.Substring(colonIndex + 1, slashIndex - colonIndex - 1));
      int denominator = int.Parse(text.Substring(slashIndex + 1));

      return new BeatTime(beat, numerator, denominator);
    }

    public static readonly BeatTime Zero = new BeatTime(0, 0, 0);

    public static readonly BeatTime Max = new BeatTime(int.MaxValue, 0, 0);

    public override string ToString()
    {
      return $"{Beat}:{Numerator}/{Denominator}";
    }

    // ==========================================
    // Comparison Operators
    // ==========================================

    public static bool operator <(BeatTime a, BeatTime b)
    {
      return a.AbsoluteValue < b.AbsoluteValue;
    }

    public static bool operator >(BeatTime a, BeatTime b)
    {
      return a.AbsoluteValue > b.AbsoluteValue;
    }

    public static bool operator <=(BeatTime a, BeatTime b)
    {
      return a.AbsoluteValue <= b.AbsoluteValue;
    }

    public static bool operator >=(BeatTime a, BeatTime b)
    {
      return a.AbsoluteValue >= b.AbsoluteValue;
    }

    public static bool operator ==(BeatTime a, BeatTime b)
    {
      return a.AbsoluteValue == b.AbsoluteValue;
    }

    public static bool operator !=(BeatTime a, BeatTime b)
    {
      return a.AbsoluteValue != b.AbsoluteValue;
    }

    // ==========================================
    // Arithmetic Operators
    // ==========================================
    public static BeatTime operator +(BeatTime a, BeatTime b)
    {
      ToImproper(a, out long n1, out long d1);
      ToImproper(b, out long n2, out long d2);
      return FromImproper(n1 * d2 + n2 * d1, d1 * d2);
    }

    public static BeatTime operator -(BeatTime a, BeatTime b)
    {
      ToImproper(a, out long n1, out long d1);
      ToImproper(b, out long n2, out long d2);
      return FromImproper(n1 * d2 - n2 * d1, d1 * d2);
    }

    public static BeatTime operator *(BeatTime a, BeatTime b)
    {
      ToImproper(a, out long n1, out long d1);
      ToImproper(b, out long n2, out long d2);
      return FromImproper(n1 * n2, d1 * d2);
    }

    public static BeatTime operator /(BeatTime a, BeatTime b)
    {
      ToImproper(a, out long n1, out long d1);
      ToImproper(b, out long n2, out long d2);
      return FromImproper(n1 * d2, d1 * n2);
    }

    private static void ToImproper(BeatTime bt, out long n, out long d)
    {
      if (bt.Denominator == 0)
      {
        n = bt.Beat;
        d = 1;
      }
      else
      {
        n = (long)bt.Beat * bt.Denominator + bt.Numerator;
        d = bt.Denominator;
      }
    }

    private static BeatTime FromImproper(long n, long d)
    {
      if (d == 0) return Zero;
      if (d < 0) { n = -n; d = -d; }
      if (n == 0) return Zero;

      long gcd = GetGCD(System.Math.Abs(n), d);
      n /= gcd;
      d /= gcd;

      if (d == 1) return new BeatTime((int)n, 0, 0);

      int beat = (int)(n / d);
      int num = (int)(n % d);
      return new BeatTime(beat, num, (int)d);
    }

    private static long GetGCD(long a, long b)
    {
      while (b != 0)
      {
        long temp = b;
        b = a % b;
        a = temp;
      }
      return a;
    }

    public override bool Equals(object obj)
    {
      if (obj is BeatTime other)
      {
        return this == other;
      }
      return false;
    }

    public override int GetHashCode()
    {
      return AbsoluteValue.GetHashCode();
    }
  }
}
