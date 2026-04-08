namespace Winithm.Core.Data
{
  public enum HitResultType
  {
    Sync,
    Delay,
    Lag,
    Timeout
  }

  /// <summary>
  /// Result of evaluating a note hit.
  /// </summary>
  public struct HitResult
  {
    /// <summary>Weight: 1.0 (Sync), 0.65 (Delay), 0.1 (Lag), 0.0 (Timeout).</summary>
    public float Weight;
    /// <summary>Timing offset in milliseconds (negative = early, positive = late).</summary>
    public float OffsetMs;
    /// <summary>The type of hit result.</summary>
    public HitResultType Type;

    /// <summary>The note that was evaluated.</summary>
    public NoteData Note;

    public bool IsHit => Weight > 0f;

    public static HitResult Miss(NoteData note) =>
      new HitResult
      {
        Weight = Constants.HitResult.ResultWeight[HitResultType.Timeout],
        OffsetMs = float.MaxValue,
        Note = note,
        Type = HitResultType.Timeout
      };

    public static HitResult FromOffset(NoteData note, float offsetMs)
    {
      float absMs = System.Math.Abs(offsetMs);
      float weight;
      HitResultType type;

      if (absMs <= Constants.HitResult.TimmingWindowMs[HitResultType.Sync])
      {
        type = HitResultType.Sync;
        weight = Constants.HitResult.ResultWeight[HitResultType.Sync];
      }
      else if (absMs <= Constants.HitResult.TimmingWindowMs[HitResultType.Delay])
      {
        type = HitResultType.Delay;
        weight = Constants.HitResult.ResultWeight[HitResultType.Delay];
      }
      else if (absMs <= Constants.HitResult.TimmingWindowMs[HitResultType.Lag])
      {
        type = HitResultType.Lag;
        weight = Constants.HitResult.ResultWeight[HitResultType.Lag];
      }
      else
      {
        type = HitResultType.Timeout;
        weight = Constants.HitResult.ResultWeight[HitResultType.Timeout];
      }

      return new HitResult { Weight = weight, OffsetMs = offsetMs, Note = note, Type = type };
    }

    /// <summary>Drag notes: within 160ms = auto 1.0.</summary>
    public static HitResult DragHit(NoteData note, float offsetMs)
    {
      float absMs = System.Math.Abs(offsetMs);
      float weight =
        absMs <= Constants.HitResult.TimmingWindowMs[HitResultType.Delay] ?
        Constants.HitResult.ResultWeight[HitResultType.Sync]
        : Constants.HitResult.ResultWeight[HitResultType.Timeout];
      HitResultType type =
        absMs <= Constants.HitResult.TimmingWindowMs[HitResultType.Delay] ?
        HitResultType.Sync
        : HitResultType.Timeout;

      return new HitResult { Weight = weight, OffsetMs = offsetMs, Note = note, Type = type };
    }
  }
}
