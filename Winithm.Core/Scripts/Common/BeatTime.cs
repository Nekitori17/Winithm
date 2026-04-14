using System;
using System.Numerics;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Deterministic timing format: B:N/D
  /// where B = base beat, N = numerator, D = denominator.
  /// Example: "0:1/2" = 0.5 beats, "1:3/4" = 1.75 beats
  /// </summary>
  public struct BeatTime : IComparable, IComparable<BeatTime>, IEquatable<BeatTime>
  {
    public int Beat { get; private set; }
    public int Numerator { get; private set; }
    public int Denominator { get; private set; }

    /// <summary>
    /// Pre-computed absolute beat value (e.g., 0:1/2 → 0.5).
    /// Uses double instead of float to avoid precision loss at large beat values.
    /// </summary>
    public double AbsoluteValue { get; private set; }

    public BeatTime(int beat, int numerator, int denominator)
    {
      Beat = beat;
      Numerator = numerator;
      Denominator = denominator;

      if (numerator != 0 && denominator == 0)
      {
        System.Diagnostics.Trace.TraceWarning(
          $"[BeatTime] Numerator={numerator} but Denominator=0, fractional beat component discarded.");
        AbsoluteValue = beat;
      }
      else
      {
        AbsoluteValue = denominator != 0 ? beat + (double)numerator / denominator : beat;
      }
    }

    // ==========================================
    // Constants
    // ==========================================

    public static readonly BeatTime Zero = new BeatTime(0, 0, 0);
    public static readonly BeatTime NaN = new BeatTime(0, 0, 0);

    /// <summary>
    /// A large sentinel value. Note: BeatTime values with a fractional part
    /// can exceed this — use only as a practical upper bound, not a true maximum.
    /// </summary>
    public static readonly BeatTime Sentinel = new BeatTime(int.MaxValue, 0, 0);

    // ==========================================
    // Parsing
    // ==========================================

    /// <summary>
    /// Parses a BeatTime string in the format "B" or "B:N/D".
    /// Throws <see cref="FormatException"/> if the input is invalid.
    /// </summary>
    public static BeatTime Parse(string text)
    {
      if (TryParse(text, out BeatTime result))
        return result;

      throw new FormatException(
        $"[BeatTime] Cannot parse \"{text}\". Expected format: \"B\" or \"B:N/D\" (e.g. \"1\", \"1:3/4\").");
    }

    /// <summary>
    /// Tries to parse a BeatTime string in the format "B" or "B:N/D".
    /// Returns false without throwing if the input is invalid.
    /// </summary>
    public static bool TryParse(string text, out BeatTime result)
    {
      result = Zero;

      if (string.IsNullOrWhiteSpace(text))
        return false;

      text = text.Trim();

      int colonIndex = text.IndexOf(':');

      // Format: "B" (integer only)
      if (colonIndex < 0)
      {
        if (!int.TryParse(text, out int beatOnly))
          return false;

        result = new BeatTime(beatOnly, 0, 0);
        return true;
      }

      int slashIndex = text.IndexOf('/');

      // Colon present but no slash → malformed
      if (slashIndex < 0 || slashIndex < colonIndex)
        return false;

      if (!int.TryParse(text.Substring(0, colonIndex), out int beat))
        return false;

      if (!int.TryParse(text.Substring(colonIndex + 1, slashIndex - colonIndex - 1), out int numerator))
        return false;

      if (!int.TryParse(text.Substring(slashIndex + 1), out int denominator))
        return false;

      if (denominator < 0)
        return false;

      result = new BeatTime(beat, numerator, denominator);
      return true;
    }

    // ==========================================
    // Formatting
    // ==========================================

    public override string ToString()
    {
      return $"{Beat}:{Numerator}/{Denominator}";
    }

    // ==========================================
    // Comparison Operators
    // ==========================================

    public static bool operator <(BeatTime a, BeatTime b) => a.CompareTo(b) < 0;
    public static bool operator >(BeatTime a, BeatTime b) => a.CompareTo(b) > 0;
    public static bool operator <=(BeatTime a, BeatTime b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BeatTime a, BeatTime b) => a.CompareTo(b) >= 0;
    public static bool operator ==(BeatTime a, BeatTime b) => a.Equals(b);
    public static bool operator !=(BeatTime a, BeatTime b) => !a.Equals(b);

    // ==========================================
    // Arithmetic Operators
    // ==========================================

    public static BeatTime operator +(BeatTime a, BeatTime b)
    {
      ToReducedFraction(a, out BigInteger n1, out BigInteger d1);
      ToReducedFraction(b, out BigInteger n2, out BigInteger d2);
      return NormalizeAndCreate((n1 * d2) + (n2 * d1), d1 * d2);
    }

    public static BeatTime operator -(BeatTime a, BeatTime b)
    {
      ToReducedFraction(a, out BigInteger n1, out BigInteger d1);
      ToReducedFraction(b, out BigInteger n2, out BigInteger d2);
      return NormalizeAndCreate((n1 * d2) - (n2 * d1), d1 * d2);
    }

    public static BeatTime operator *(BeatTime a, BeatTime b)
    {
      ToReducedFraction(a, out BigInteger n1, out BigInteger d1);
      ToReducedFraction(b, out BigInteger n2, out BigInteger d2);
      return NormalizeAndCreate(n1 * n2, d1 * d2);
    }

    /// <summary>Divides a by b. Throws <see cref="DivideByZeroException"/> if b is zero.</summary>
    public static BeatTime operator /(BeatTime a, BeatTime b)
    {
      if (b == Zero)
        throw new DivideByZeroException("[BeatTime] Cannot divide by zero (b is BeatTime.Zero).");

      ToReducedFraction(a, out BigInteger n1, out BigInteger d1);
      ToReducedFraction(b, out BigInteger n2, out BigInteger d2);
      return NormalizeAndCreate(n1 * d2, d1 * n2);
    }

    // ==========================================
    // IComparable / IEquatable
    // ==========================================

    public int CompareTo(BeatTime other)
    {
      ToReducedFraction(this, out BigInteger ln, out BigInteger ld);
      ToReducedFraction(other, out BigInteger rn, out BigInteger rd);
      return (ln * rd).CompareTo(rn * ld);
    }

    public int CompareTo(object obj)
    {
      if (obj == null)
        return 1;

      if (obj is BeatTime other)
        return CompareTo(other);

      throw new ArgumentException("Object must be of type BeatTime.", nameof(obj));
    }

    public bool Equals(BeatTime other) => CompareTo(other) == 0;

    public override bool Equals(object obj)
    {
      if (obj is BeatTime other)
        return Equals(other);

      return false;
    }

    public override int GetHashCode()
    {
      ToReducedFraction(this, out BigInteger numerator, out BigInteger denominator);
      return unchecked((numerator.GetHashCode() * 397) ^ denominator.GetHashCode());
    }

    // ==========================================
    // Private Helpers
    // ==========================================

    /// <summary>
    /// Converts a BeatTime to its fully reduced improper fraction (n/d, d > 0).
    /// The stored Beat/Numerator/Denominator are already normalized by the constructor,
    /// so this is a cheap reassembly — no GCD computation needed here.
    /// </summary>
    private static void ToReducedFraction(BeatTime bt, out BigInteger numerator, out BigInteger denominator)
    {
      if (bt.Denominator == 0)
      {
        numerator = bt.Beat;
        denominator = BigInteger.One;
        return;
      }

      numerator = (BigInteger)bt.Beat * bt.Denominator + bt.Numerator;
      denominator = bt.Denominator;

      // Denominator is always positive coming out of NormalizeAndCreate,
      // but guard defensively in case of direct constructor usage.
      if (denominator.Sign < 0)
      {
        numerator = -numerator;
        denominator = -denominator;
      }
    }

    /// <summary>
    /// Takes an arbitrary improper fraction n/d produced by arithmetic,
    /// reduces it to lowest terms, then splits into the canonical
    /// Beat + (Numerator/Denominator) mixed-number form.
    /// </summary>
    private static BeatTime NormalizeAndCreate(BigInteger n, BigInteger d)
    {
      if (d.IsZero)
        throw new DivideByZeroException("[BeatTime] Result has a zero denominator.");

      // Ensure denominator is positive.
      if (d.Sign < 0)
      {
        n = -n;
        d = -d;
      }

      if (n.IsZero)
        return Zero;

      // Reduce to lowest terms.
      BigInteger gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(n), d);
      n /= gcd;
      d /= gcd;

      // Whole number result.
      if (d.IsOne)
        return new BeatTime((int)n, 0, 0);

      // Split into beat + positive remainder (floor division).
      BigInteger beat = BigInteger.DivRem(n, d, out BigInteger remainder);
      if (remainder.Sign < 0)
      {
        beat -= BigInteger.One;
        remainder += d;
      }

      return new BeatTime((int)beat, (int)remainder, (int)d);
    }
  }
}
