using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Holds the baked prefix sum distances for a single time frame.
  /// </summary>
  public class FrameCache
  {
    internal double CachedBeat = double.NaN;
    internal float[] PrefixDistance = Array.Empty<float>();
  }

  /// <summary>
  /// Manages scroll speed segments and visual lane distances.
  /// </summary>
  public class SpeedStepManager : IDeepCloneable<SpeedStepManager>
  {
    public event Action<SpeedStepManager> OnUpdated;

    private int _updateLockCount = 0;

    /// <summary>
    /// Suspends notifications to allow batch edits.
    /// </summary>
    public void BeginUpdate() => _updateLockCount++;

    /// <summary>
    /// Resumes notifications and clears frame cache if edits were made.
    /// </summary>
    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success)
      {
        if (FrameCache != null) FrameCache.CachedBeat = double.NaN;
        OnUpdated?.Invoke(this);
      }
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0)
      {
        if (FrameCache != null) FrameCache.CachedBeat = double.NaN;
        OnUpdated?.Invoke(this);
      }
    }

    public List<SpeedStepData> SpeedStepCollection { get; private set; } = new List<SpeedStepData>();
    public FrameCache FrameCache { get; private set; } = new FrameCache();

    public SpeedStepManager DeepClone(ObjectFactory objectFactory, BeatTime? offset)
    {
      var newSpeedStep = new SpeedStepManager();

      newSpeedStep.BeginUpdate();

      foreach (var speedStep in SpeedStepCollection)
        newSpeedStep.AddSpeedStep(speedStep.DeepClone(objectFactory, offset));

      newSpeedStep.EndUpdate();

      return newSpeedStep;
    }

    private void SubscribeChangeSpeedSteps(SpeedStepData speedStep)
    {
      speedStep.OnStartBeatChanged -= HandleStartBeatChanged;
      speedStep.OnStartBeatChanged += HandleStartBeatChanged;

      speedStep.OnUpdated -= HandleUpdated;
      speedStep.OnUpdated += HandleUpdated;
    }

    private void UnSubscribeChangeSpeedSteps(SpeedStepData speedStep)
    {
      speedStep.OnStartBeatChanged -= HandleStartBeatChanged;
      speedStep.OnUpdated -= HandleUpdated;
    }

    private void HandleStartBeatChanged(SpeedStepData speedStep)
    {
      RemoveSpeedStep(speedStep);
      AddSpeedStep(speedStep);
    }

    private void HandleUpdated(SpeedStepData speedStep)
    {
      NotifyChanged();
    }

    public int AddSpeedStep(SpeedStepData speedStep)
    {
      int index = FindAddIndex(speedStep);
      SpeedStepCollection.Insert(index, speedStep);
      SubscribeChangeSpeedSteps(speedStep);

      NotifyChanged();
      return index;
    }

    public int[] AddSpeedSteps(IEnumerable<SpeedStepData> speedSteps)
    {
      if (!speedSteps.Any()) return Array.Empty<int>();

      BeginUpdate();

      int[] indices = new int[speedSteps.Count()];
      for (int i = 0; i < speedSteps.Count(); i++)
        indices[i] = AddSpeedStep(speedSteps.ElementAt(i));

      EndUpdate();

      return indices;
    }

    public bool RemoveSpeedStep(SpeedStepData speedStep)
    {
      if (!SpeedStepCollection.Remove(speedStep)) return false;
      UnSubscribeChangeSpeedSteps(speedStep);

      NotifyChanged();
      return true;
    }

    public int RemoveSpeedSteps(IEnumerable<SpeedStepData> speedSteps)
    {
      if (!speedSteps.Any()) return 0;

      BeginUpdate();

      int removedCount = 0;
      foreach (var speedStep in speedSteps)
        if (RemoveSpeedStep(speedStep)) removedCount++;

      EndUpdate();

      return removedCount;
    }

    public bool RemoveSpeedStep(string id)
    {
      if (string.IsNullOrEmpty(id)) return false;
      
      var toRemove = SpeedStepCollection.FindAll(st => st.ID == id);
      if (toRemove.Count == 0) return false;

      foreach (var st in toRemove) UnSubscribeChangeSpeedSteps(st);
      SpeedStepCollection.RemoveAll(x => x.ID == id);

      NotifyChanged();
      return true;
    }

    public int RemoveSpeedSteps(List<string> ids)
    {
      if (ids.Count == 0) return 0;

      BeginUpdate();

      int removedCount = 0;
      foreach (var id in ids)
        if (RemoveSpeedStep(id)) removedCount++;

      EndUpdate();

      return removedCount;
    }


    public SpeedStepData GetSpeedStep(string id)
    {
      if (string.IsNullOrEmpty(id)) return null;

      var speedStep = SpeedStepCollection.FirstOrDefault(st => st.ID == id);

      if (speedStep == default) return null;
      return speedStep;
    }

    public IReadOnlyList<SpeedStepData> GetSpeedSteps(IEnumerable<string> ids)
    {
      if (!ids.Any()) return Array.Empty<SpeedStepData>();

      var result = new List<SpeedStepData>();
      foreach (var id in ids)
      {
        var speedStep = GetSpeedStep(id);
        if (speedStep != null) result.Add(speedStep);
      }

      return result;
    }

    public SpeedStepData GetFirst()
    {
      if (SpeedStepCollection.Count == 0) return null;
      return SpeedStepCollection[0];
    }

    public SpeedStepData GetLast()
    {
      if (SpeedStepCollection.Count == 0) return null;
      return SpeedStepCollection[SpeedStepCollection.Count - 1];
    }

    /// <summary>
    /// Rebuilds the distance cache for the current beat.
    /// </summary>
    public void BakeFrameCache(double currentBeat)
    {
      if (FrameCache == null || SpeedStepCollection.Count == 0) return;

      // Rebuild only when the beat has actually changed.
      if (FrameCache.CachedBeat == currentBeat) return;

      FrameCache.CachedBeat = currentBeat;

      int n = SpeedStepCollection.Count;
      if (FrameCache.PrefixDistance.Length < n)
        FrameCache.PrefixDistance = new float[n];

      // Origin is at steps[0].Start
      FrameCache.PrefixDistance[0] = 0f;

      for (int i = 1; i < n; i++)
      {
        double segStart = SpeedStepCollection[i - 1].StartBeat.AbsoluteValue;
        double segEnd = SpeedStepCollection[i].StartBeat.AbsoluteValue;
        double segLen = segEnd - segStart;

        float speed = EvaluateSpeed(SpeedStepCollection[i - 1], currentBeat);
        FrameCache.PrefixDistance[i] = FrameCache.PrefixDistance[i - 1] + speed * (float)segLen;
      }
    }

    /// <summary>
    /// Returns the visual offset of targetBeat relative to currentBeat.
    /// </summary>
    public float GetVisualOffset(float currentBeat, float targetBeat)
    {
      if (Math.Abs(currentBeat - targetBeat) < 0.0001f) return 0f;

      if (FrameCache == null) FrameCache = new FrameCache();
      if (FrameCache.CachedBeat != currentBeat) BakeFrameCache(currentBeat);

      double laneStart = SpeedStepCollection.Count > 0 ? SpeedStepCollection[0].StartBeat.AbsoluteValue : 0.0;

      // Clamp both ends to lane start — no visual meaning before spawn.
      double clampedCurrent = Math.Max(currentBeat, laneStart);
      double clampedTarget = Math.Max(targetBeat, laneStart);
      if (Math.Abs(clampedCurrent - clampedTarget) < 0.0001f) return 0f;

      float distCurrent = DistanceFromOrigin(currentBeat, clampedCurrent);
      float distTarget = DistanceFromOrigin(currentBeat, clampedTarget);

      return (targetBeat >= currentBeat)
        ? (distTarget - distCurrent)
        : -(distTarget - distCurrent);
    }

    /// <summary>
    /// Calculates current scroll speed multiplier at a specific beat.
    /// </summary>
    public float GetSpeedAt(double currentBeat, double beat)
    {
      if (SpeedStepCollection == null || SpeedStepCollection.Count == 0) return 1f;
      int idx = FindStepIndex(beat);
      return EvaluateSpeed(SpeedStepCollection[idx], currentBeat);
    }

    private float DistanceFromOrigin(double currentBeat, double beat)
    {
      if (SpeedStepCollection.Count == 0) return 0f;

      double laneStart = SpeedStepCollection[0].StartBeat.AbsoluteValue;
      if (beat <= laneStart) return 0f;

      int n = SpeedStepCollection.Count;

      // Beat is past the last step — extend with last step's speed.
      double lastStart = SpeedStepCollection[n - 1].StartBeat.AbsoluteValue;
      if (beat >= lastStart)
      {
        double tail = beat - lastStart;
        float speed = EvaluateSpeed(SpeedStepCollection[n - 1], currentBeat);
        return FrameCache.PrefixDistance[n - 1] + speed * (float)tail;
      }

      int idx = FindStepIndex(beat);

      double segStart = SpeedStepCollection[idx].StartBeat.AbsoluteValue;
      double tail2 = beat - segStart;
      float speed2 = EvaluateSpeed(SpeedStepCollection[idx], currentBeat);
      return FrameCache.PrefixDistance[idx] + speed2 * (float)tail2;
    }

    private static float EvaluateSpeed(SpeedStepData step, double currentBeat)
    {
      return step.StoryboardEvents.Evaluate(StoryboardProperty.Speed, currentBeat, new AnyValue(step.Multiplier)).X;
    }

    public int FindAddIndex(SpeedStepData speedStep)
    {
      if (SpeedStepCollection.Count == 0) return 0;

      int left = 0, right = SpeedStepCollection.Count - 1;
      while (left <= right)
      {
        int mid = left + (right - left) / 2;
        if (SpeedStepCollection[mid].StartBeat <= speedStep.StartBeat) left = mid + 1;
        else right = mid - 1;
      }
      return left;
    }

    private int FindStepIndex(double targetBeat)
    {
      if (SpeedStepCollection.Count == 0) return -1;
      if (targetBeat < SpeedStepCollection[0].StartBeat.AbsoluteValue) return 0;

      int lo = 0, hi = SpeedStepCollection.Count - 1;
      while (lo < hi)
      {
        int mid = (lo + hi + 1) / 2;
        if (SpeedStepCollection[mid].StartBeat.AbsoluteValue <= targetBeat) lo = mid;
        else hi = mid - 1;
      }
      return lo;
    }
  }
}