using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.Logic
{
  /// <summary>
  /// Evaluates per-property StoryboardEvent lists at any given beat time.
  /// Each list is assumed to be sorted by Start.AbsoluteValue (ascending).
  /// Uses binary search for O(log n) event lookup.
  /// </summary>
  public static class StoryboardEvaluator
  {
    /// <summary>
    /// Evaluate any property at the given beat.
    /// The events list must be pre-sorted by Start.AbsoluteValue.
    /// </summary>
    public static AnyValue Evaluate(List<StoryboardEvent> events, float currentBeat, AnyValue defaultValue)
    {
      if (events == null || events.Count == 0) return defaultValue;

      // Binary search: find the last event where Start.AbsoluteValue <= currentBeat
      int idx = FindLastStarted(events, currentBeat);
      if (idx < 0) return defaultValue;

      var evt = events[idx];
      float endBeat = evt.EndBeat;

      if (currentBeat >= endBeat)
        return evt.To;

      // Interpolate
      float startBeat = evt.Start.AbsoluteValue;
      float length = endBeat - startBeat;
      float t = length > 0f ? (currentBeat - startBeat) / length : 1f;
      t = evt.Easing == EasingType.Bezier
        ? EasingFunctions.EvaluateBezier(evt.EasingBezier, t)
        : EasingFunctions.Evaluate(evt.Easing, t);

      AnyValue from = evt.From;
      if (from.Type == AnyValueType.Inherited) from = defaultValue;
        
      return AnyValue.Lerp(from, evt.To, t);
    }

    /// <summary>
    /// Binary search: find the index of the last event whose Start.AbsoluteValue &lt;= currentBeat.
    /// Returns -1 if no event has started yet.
    /// Events must be sorted by Start.AbsoluteValue ascending.
    /// </summary>
    private static int FindLastStarted(List<StoryboardEvent> events, float currentBeat)
    {
      int left = 0, right = events.Count - 1, best = -1;

      while (left <= right)
      {
        int mid = left + (right - left) / 2;
        if (events[mid].Start.AbsoluteValue <= currentBeat)
        {
          best = mid;
          left = mid + 1;
        }
        else
        {
          right = mid - 1;
        }
      }

      return best;
    }
  }
}
