using System;
using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.Logic
{
  /// <summary>
  /// Manages visual scroll offset calculations with per-frame prefix sum caching.
  /// Evaluation is based on the current beat to handle rubber-band speed stretching.
  /// </summary>
  public static class SpeedCalculator
  {
    // ---------------------------------------------------------------
    // Per-window frame cache
    // ---------------------------------------------------------------

    /// <summary>
    /// Holds the baked prefix sum distances for a single frame.
    /// </summary>
    public class FrameCache
    {
      internal float CachedBeat = float.NaN;
      internal float[] PrefixDistance = Array.Empty<float>();
    }

    /// <summary>
    /// Rebuilds the distance cache for the current beat.
    /// </summary>
    public static void BakeFrameCache(FrameCache cache, List<SpeedStep> steps, float currentBeat)
    {
      if (cache == null || steps == null || steps.Count == 0) return;

      // Rebuild only when the beat has actually changed.
      if (cache.CachedBeat == currentBeat) return;

      cache.CachedBeat = currentBeat;

      int n = steps.Count;
      if (cache.PrefixDistance.Length < n)
        cache.PrefixDistance = new float[n];

      // PrefixDistance[0] = 0: origin is at steps[0].Start.
      cache.PrefixDistance[0] = 0f;

      for (int i = 1; i < n; i++)
      {
        float segStart = steps[i - 1].Start.AbsoluteValue;
        float segEnd = steps[i].Start.AbsoluteValue;
        float segLen = segEnd - segStart;
        float speed = EvaluateSpeed(steps[i - 1], currentBeat);
        cache.PrefixDistance[i] = cache.PrefixDistance[i - 1] + speed * segLen;
      }
    }

    /// <summary>
    /// Returns the visual offset of targetBeat relative to currentBeat.
    /// </summary>
    public static float GetVisualOffset(FrameCache cache, List<SpeedStep> steps, float currentBeat, float targetBeat)
    {
      if (steps == null || steps.Count == 0) return 0f;
      if (Math.Abs(currentBeat - targetBeat) < 0.0001f) return 0f;

      // Auto-bake if caller skipped it.
      if (cache == null) cache = new FrameCache();
      if (cache.CachedBeat != currentBeat)
        BakeFrameCache(cache, steps, currentBeat);

      float laneStart = steps[0].Start.AbsoluteValue;

      // Clamp both ends to lane start — no visual meaning before spawn.
      float clampedCurrent = Math.Max(currentBeat, laneStart);
      float clampedTarget = Math.Max(targetBeat, laneStart);
      if (Math.Abs(clampedCurrent - clampedTarget) < 0.0001f) return 0f;

      float distCurrent = DistanceFromOrigin(cache, steps, currentBeat, clampedCurrent);
      float distTarget = DistanceFromOrigin(cache, steps, currentBeat, clampedTarget);

      return (targetBeat >= currentBeat)
        ? (distTarget - distCurrent)
        : -(distTarget - distCurrent);
    }

    /// <summary>
    /// Calculates current scroll speed at a specific beat.
    /// </summary>
    public static float GetSpeedAt(List<SpeedStep> steps, float beat, float currentBeat)
    {
      if (steps == null || steps.Count == 0) return 1f;
      int idx = FindStepIndex(steps, beat);
      return EvaluateSpeed(steps[idx], currentBeat);
    }

    /// <summary>
    /// Calculates cumulative visual distance from origin to beat.
    /// </summary>
    private static float DistanceFromOrigin(FrameCache cache, List<SpeedStep> steps, float currentBeat, float beat)
    {
      float laneStart = steps[0].Start.AbsoluteValue;
      if (beat <= laneStart) return 0f;

      int n = steps.Count;

      // Beat is past the last step — extend with last step's speed.
      float lastStart = steps[n - 1].Start.AbsoluteValue;
      if (beat >= lastStart)
      {
        float tail = beat - lastStart;
        float speed = EvaluateSpeed(steps[n - 1], currentBeat);
        return cache.PrefixDistance[n - 1] + speed * tail;
      }

      // Binary search: find i such that steps[i].Start <= beat < steps[i+1].Start.
      int idx = FindStepIndex(steps, beat);

      float segStart = steps[idx].Start.AbsoluteValue;
      float tail2 = beat - segStart;
      float speed2 = EvaluateSpeed(steps[idx], currentBeat);
      return cache.PrefixDistance[idx] + speed2 * tail2;
    }

    /// <summary>
    /// Evaluates speed for a specific step.
    /// </summary>
    private static float EvaluateSpeed(SpeedStep step, float currentBeat)
    {
      return StoryboardEvaluator.EvaluateFloat(
        step.Events, StoryboardProperty.Speed, currentBeat, step.Multiplier);
    }

    /// <summary>
    /// Binary search for the stepIndex containing the beat.
    /// </summary>
    private static int FindStepIndex(List<SpeedStep> steps, float beat)
    {
      int left = 0, right = steps.Count - 1, best = 0;

      while (left <= right)
      {
        int mid = left + (right - left) / 2;
        if (steps[mid].Start.AbsoluteValue <= beat)
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