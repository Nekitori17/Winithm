using System;

namespace Winithm.Core.Interfaces
{
  public interface IScoreProvider
  {
    event Action Updated;

    int Score { get; }
    float Accuracy { get; }
    int Combo { get; }
    int MaxCombo { get; }
    int TotalHittableNotes { get; }
    int NotesEvaluated { get; }
    int PerfectCount { get; }
    int GoodCount { get; }
    int BadCount { get; }
    int MissCount { get; }
  }
}
