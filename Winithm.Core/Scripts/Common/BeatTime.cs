namespace Winithm.Core.Common
{
  /// <summary>
  /// Deterministic timing format: B:N/D
  /// where B = base beat, N = numerator, D = denominator.
  /// Example: "0:1/2" = 0.5 beats, "1:3/4" = 1.75 beats
  /// </summary>
  public struct BeatTime
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
        Godot.GD.PushWarning($"[BeatTime] Numerator={numerator} but Denominator=0, fractional beat component discarded.");
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

    public override string ToString()
    {
      return $"{Beat}:{Numerator}/{Denominator}";
    }
  }
}
