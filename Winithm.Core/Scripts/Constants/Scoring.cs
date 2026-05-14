using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Constants
{
  public static class Scoring
  {
    public enum Grade
    {
      AP,
      FC,
      S,
      A,
      B,
      C,
      D,
      F
    }

    public static readonly Dictionary<Grade, int> GradeMinimumScore = new Dictionary<Grade, int>
    {
      { Grade.AP, 1_000_000 },
      { Grade.FC, 1_000_000 },
      { Grade.S, 950_000 },
      { Grade.A, 875_000 },
      { Grade.B, 800_000 },
      { Grade.C, 700_000 },
      { Grade.D, 500_000 },
      { Grade.F, 0 }
    };

    public static readonly Dictionary<Grade, string> GradeNames = new Dictionary<Grade, string>
    {
      { Grade.AP, "GFX" },
      { Grade.FC, "VFX" },
      { Grade.S, "FX" },
      { Grade.A, "A" },
      { Grade.B, "B" },
      { Grade.C, "C" },
      { Grade.D, "D" },
      { Grade.F, "F" }
    };
  }
}