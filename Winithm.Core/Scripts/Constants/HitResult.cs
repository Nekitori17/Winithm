using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Constants
{
  public static class HitResult
  {
    public static readonly Dictionary<HitResultType, double> TimmingWindowMs = new Dictionary<HitResultType, double>
    {
      { HitResultType.Perfect, 65 },
      { HitResultType.Good, 125 },
      { HitResultType.Bad, 175 },
      // Miss is not a timing window, but a flag for notes that were not hit.
      { HitResultType.Miss, 175 },
    };

    public static readonly Dictionary<HitResultType, float> ResultWeight = new Dictionary<HitResultType, float>
    {
      { HitResultType.Perfect, 1f },
      { HitResultType.Good, 0.65f },
      { HitResultType.Bad, 0.1f },
      { HitResultType.Miss, 0f },
    };
  }
}