using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.Logic
{
  /// <summary>
  /// Evaluates per-property StoryboardEvent lists at any given beat time.
  /// Each list is assumed to be sorted by Start.AbsoluteValue (ascending).
  /// Uses a forward-advancing cursor for O(1) amortized lookups during normal playback.
  /// </summary>
  public static class StoryboardEvaluator
  {
    /// <summary>
    /// Per-list cursor that tracks the last active event index.
    /// Amortized O(1) for forward-only playback; auto-resets on seek/rewind.
    /// </summary>
    public class Cursor
    {
      internal int LastIndex;

      public Cursor()
      {
        LastIndex = 0;
      }

      /// <summary>Resets the cursor to the beginning.</summary>
      public void Reset()
      {
        LastIndex = 0;
      }
    }

    /// <summary>
    /// Evaluate any property at the given beat (stateless binary search, legacy path).
    /// </summary>
    public static AnyValue Evaluate(List<StoryboardEvent> events, float currentBeat, AnyValue defaultValue)
    {
      if (events == null || events.Count == 0) return defaultValue;

      int idx = FindLastStarted(events, currentBeat);
      if (idx < 0) return defaultValue;

      return Interpolate(events[idx], currentBeat, defaultValue);
    }

    /// <summary>
    /// Evaluate any property at the given beat using a cursor for O(1) amortized lookup.
    /// The cursor tracks the last-used index and advances forward without re-searching.
    /// Falls back to binary search only on seek/rewind.
    /// </summary>
    public static AnyValue Evaluate(List<StoryboardEvent> events, float currentBeat, AnyValue defaultValue, Cursor cursor)
    {
      if (events == null || events.Count == 0) return defaultValue;

      int idx = AdvanceCursor(events, currentBeat, cursor);
      if (idx < 0) return defaultValue;

      return Interpolate(events[idx], currentBeat, defaultValue);
    }

    /// <summary>
    /// Interpolates the value of a single event at the given beat.
    /// </summary>
    private static AnyValue Interpolate(StoryboardEvent evt, float currentBeat, AnyValue defaultValue)
    {
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

    // ──────────────────────────────────────────────
    //  Cursor-based lookup (O(1) amortized)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Advances the cursor forward to find the last event whose Start &lt;= currentBeat.
    /// If the beat went backwards (seek/rewind), falls back to binary search and resets cursor.
    /// Returns -1 if no event has started yet.
    /// </summary>
    private static int AdvanceCursor(List<StoryboardEvent> events, float currentBeat, Cursor cursor)
    {
      int n = events.Count;
      int last = cursor.LastIndex;

      // Bounds check: cursor might be stale after event list changes
      if (last >= n) last = n - 1;

      // Case 1: Beat went backwards — binary search reset
      if (last > 0 && events[last].Start.AbsoluteValue > currentBeat)
      {
        int idx = FindLastStarted(events, currentBeat);
        cursor.LastIndex = idx >= 0 ? idx : 0;
        return idx;
      }

      // Case 2: Forward scan — advance cursor until we overshoot
      while (last + 1 < n && events[last + 1].Start.AbsoluteValue <= currentBeat)
      {
        last++;
      }

      cursor.LastIndex = last;

      // Verify the cursor event has actually started
      if (events[last].Start.AbsoluteValue > currentBeat)
        return -1;

      return last;
    }

    // ──────────────────────────────────────────────
    //  Binary Search (fallback for seek/rewind)
    // ──────────────────────────────────────────────

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
