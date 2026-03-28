using Winithm.Core.Data;

namespace Winithm.Client.Data
{
  /// <summary>
  /// Result of evaluating a note hit.
  /// </summary>
  public struct HitResult
  {
    /// <summary>Weight: 1.0 (Perfect), 0.5 (Great), 0.1 (Good), 0.0 (Miss).</summary>
    public float Weight;
    /// <summary>Timing offset in milliseconds (negative = early, positive = late).</summary>
    public float OffsetMs;
    /// <summary>The note that was evaluated.</summary>
    public NoteData Note;

    public bool IsHit => Weight > 0f;

    public static HitResult Miss(NoteData note) => new HitResult { Weight = 0f, OffsetMs = float.MaxValue, Note = note };
    public static HitResult FromOffset(NoteData note, float offsetMs)
    {
      float absMs = System.Math.Abs(offsetMs);
      float weight;
      if (absMs <= 50f) weight = 1.0f;
      else if (absMs <= 150f) weight = 0.5f;
      else if (absMs <= 200f) weight = 0.1f;
      else weight = 0f; // No reaction — too far

      return new HitResult { Weight = weight, OffsetMs = offsetMs, Note = note };
    }

    /// <summary>Drag notes: within 100ms = auto 1.0.</summary>
    public static HitResult DragHit(NoteData note, float offsetMs)
    {
      float absMs = System.Math.Abs(offsetMs);
      float weight = absMs <= 100f ? 1.0f : 0f;
      return new HitResult { Weight = weight, OffsetMs = offsetMs, Note = note };
    }
  }
}
