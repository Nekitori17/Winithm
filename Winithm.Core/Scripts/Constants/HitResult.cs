using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Constants
{
  public static class HitResult
  {
    public static readonly Dictionary<HitResultType, float> TimmingWindowMs = new Dictionary<HitResultType, float>
    {
      { HitResultType.Sync, 60f },
      { HitResultType.Delay, 120f },
      { HitResultType.Lag, 180f },
      // Timeout is not a timing window, but a flag for notes that were not hit.
      { HitResultType.Timeout, 180f },
    };

    public static readonly Dictionary<HitResultType, float> ResultWeight = new Dictionary<HitResultType, float>
    {
      { HitResultType.Sync, 1f },
      { HitResultType.Delay, 0.65f },
      { HitResultType.Lag, 0.1f },
      { HitResultType.Timeout, 0f },
    };
  }
}