using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Constants
{
  public static class HitResult
  {
    public static readonly Dictionary<HitResultType, double> TimmingWindowMs = new Dictionary<HitResultType, double>
    {
      { HitResultType.Perfect, 75 },
      { HitResultType.Good, 150 },
      { HitResultType.Bad, 200 },
      // Miss is not a timing window, but a flag for notes that were not hit.
      { HitResultType.Miss, 200 },
    };

    public static readonly Dictionary<HitResultType, float> ResultWeight = new Dictionary<HitResultType, float>
    {
      { HitResultType.Perfect, 1f },
      { HitResultType.Good, 0.65f },
      { HitResultType.Bad, 0.1f },
      { HitResultType.Miss, 0f },
    };

    public static readonly Dictionary<HitResultType, string> HitResultNames = new Dictionary<HitResultType, string>
    {
      { HitResultType.Perfect, "Sharp" },
      { HitResultType.Good, "Clear" },
      { HitResultType.Bad, "Blur" },
      { HitResultType.Miss, "Lost" },
    };
  }
}