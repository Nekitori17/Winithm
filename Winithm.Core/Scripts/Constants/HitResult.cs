using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Constants
{
  public static class HitResult
  {
    public static readonly Dictionary<HitResultType, float> TimmingWindowMs = new Dictionary<HitResultType, float>
    {
      { HitResultType.Perfect, 65f },
      { HitResultType.Good, 125f },
      { HitResultType.Bad, 175f },
      // Miss is not a timing window, but a flag for notes that were not hit.
      { HitResultType.Miss, 175f },
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