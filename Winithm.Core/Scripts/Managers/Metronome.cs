using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Common;
using System;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages BPM changes and converts between time and beats deterministically.
  /// </summary>
  public class Metronome
  {
    public event Action<Metronome> OnMetronomeChanged;

    private int _updateLockCount = 0;
    private int _minRecalculateIdx = int.MaxValue;

    /// <summary>
    /// Suspends event notifications and delays prefix calculation to allow multiple property edits without overhead.
    /// </summary>
    public void BeginUpdate() => _updateLockCount++;

    /// <summary>
    /// Resumes event notifications and recalculates state if edits were made.
    /// </summary>
    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success)
      {
        CommitCalculations();
        OnMetronomeChanged?.Invoke(this);
      }
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0)
      {
        CommitCalculations();
        OnMetronomeChanged?.Invoke(this);
      }
    }

    /// <summary>
    /// Flags the starting index that needs to be recomputed upon the next unlock.
    /// </summary>
    private void RequestRecalculate(int index)
    {
      if (index < _minRecalculateIdx) _minRecalculateIdx = index;
    }

    private void CommitCalculations()
    {
      if (_minRecalculateIdx != int.MaxValue)
      {
        RecalculateFrom(_minRecalculateIdx);
        _minRecalculateIdx = int.MaxValue;
      }
    }

    public BaseBPM BaseBPM { get; private set; } = new BaseBPM(0, 120, 4);
    public List<BPMStop> BPMStops { get; private set; } = new List<BPMStop>();

    public int StopCount => BPMStops.Count;

    public Metronome()
    {
      BaseBPM.OnBeatChanged += (bb) => { RequestRecalculate(0); NotifyChanged(); };
      BaseBPM.OnDataChanged += (bb) => NotifyChanged();
    }

    /// <summary>
    /// Force recalculates all cached expected start times.
    /// </summary>
    public void Compute() => RecalculateFrom(0);

    private void RecalculateFrom(int index)
    {
      if (index < 0) index = 0;

      for (int i = index; i < BPMStops.Count; i++)
      {
        var curr = BPMStops[i];
        if (i == 0)
        {
          double beatDiff = curr.StartBeat.AbsoluteValue;
          curr.StartTimeSeconds = BaseBPM.BaseOffsetSeconds + (beatDiff / BaseBPM.BeatsPerSecond);
        }
        else
        {
          var prev = BPMStops[i - 1];
          double beatDiff = curr.StartBeat.AbsoluteValue - prev.StartBeat.AbsoluteValue;
          curr.StartTimeSeconds = prev.StartTimeSeconds + (beatDiff / prev.BeatsPerSecond);
        }
      }
    }

    private void SubscribeChangeBPMStops(BPMStop bPMStop)
    {
      bPMStop.OnStartBeatChanged -= HandleStartBeatChanged;
      bPMStop.OnStartBeatChanged += HandleStartBeatChanged;

      bPMStop.OnBPMChanged -= HandleBPMChanged;
      bPMStop.OnBPMChanged += HandleBPMChanged;

      bPMStop.OnDataChanged -= HandleDataChanged;
      bPMStop.OnDataChanged += HandleDataChanged;
    }

    private void UnSubscribeChangeBPMStops(BPMStop bPMStop)
    {
      bPMStop.OnStartBeatChanged -= HandleStartBeatChanged;
      bPMStop.OnBPMChanged -= HandleBPMChanged;
      bPMStop.OnDataChanged -= HandleDataChanged;
    }

    public void SetBaseBPM(BaseBPM baseBPM)
    {
      if (BaseBPM.BaseOffsetSeconds == baseBPM.BaseOffsetSeconds &&
          BaseBPM.InitialBPM == baseBPM.InitialBPM &&
          BaseBPM.TimeSignature == baseBPM.TimeSignature) return;

      BaseBPM = baseBPM;
      RequestRecalculate(0);
      NotifyChanged();
    }

    public BaseBPM GetBaseBPM() => BaseBPM;

    public int AddBPMStop(BPMStop bPMStop)
    {
      int idx = FindAddIndex(bPMStop);
      BPMStops.Insert(idx, bPMStop);
      SubscribeChangeBPMStops(bPMStop);

      RequestRecalculate(idx);
      NotifyChanged();
      
      return idx;
    }

    public int[] AddBPMStops(List<BPMStop> bPMStops)
    {
      if (bPMStops.Count == 0) return Array.Empty<int>();

      BeginUpdate();

      int[] indices = new int[bPMStops.Count];
      for (int i = 0; i < bPMStops.Count; i++)
        indices[i] = AddBPMStop(bPMStops[i]);
      
      EndUpdate();

      return indices;
    }

    public bool RemoveBPMStop(BPMStop bPMStop)
    {
      int idx = BPMStops.IndexOf(bPMStop);
      if (idx == -1) return false;

      BPMStops.RemoveAt(idx);
      UnSubscribeChangeBPMStops(bPMStop);
      
      RequestRecalculate(idx);
      NotifyChanged();
      
      return true;
    }

    public int RemoveBPMStops(List<BPMStop> bPMStops)
    {
      if (bPMStops.Count == 0) return 0;

      BeginUpdate();

      int removedCount = 0;
      for (int i = 0; i < bPMStops.Count; i++)
      {
        if (RemoveBPMStop(bPMStops[i])) removedCount++;
      }
      
      EndUpdate(removedCount > 0);

      return removedCount;
    }

    public BPMStop GetBPMStop(BPMStop bPMStop)
    {
      if (BPMStops.Contains(bPMStop)) return bPMStop;
      throw new InvalidOperationException($"BPM stop not found: {bPMStop}");
    }

    public List<BPMStop> GetBPMStops(List<BPMStop> bPMStops)
    {
      var result = new List<BPMStop>();
      foreach (var bPMStop in bPMStops)
      {
        if (BPMStops.Contains(bPMStop))
        {
          result.Add(bPMStop);
        }
      }
      return result;
    }

    public float GetBPMAtBeat(double beat)
    {
      int idx = FindStopIndex(beat);
      return idx == -1 ? BaseBPM.InitialBPM : BPMStops[idx].BPM;
    }

    public int GetTimeSignatureAtBeat(double beat)
    {
      int idx = FindStopIndex(beat);
      return idx == -1 ? BaseBPM.TimeSignature : BPMStops[idx].TimeSignature;
    }

    private void HandleStartBeatChanged(BPMStop bPMStop)
    {
      RemoveBPMStop(bPMStop);
      AddBPMStop(bPMStop);
    }

    private void HandleBPMChanged(BPMStop bPMStop)
    {
      int idx = BPMStops.IndexOf(bPMStop);
      if (idx == -1) return;

      RequestRecalculate(idx);
      NotifyChanged();
    }

    private void HandleDataChanged(BPMStop bPMStop) => NotifyChanged();

    public double ToSeconds(double beat)
    {
      int idx = FindStopIndex(beat);

      if (idx == -1)
      {
        return BaseBPM.BaseOffsetSeconds + (beat / BaseBPM.BeatsPerSecond);
      }

      var stop = BPMStops[idx];
      double deltaBeat = beat - stop.StartBeat.AbsoluteValue;
      return stop.StartTimeSeconds + (deltaBeat / stop.BeatsPerSecond);
    }

    public double ToBeat(double seconds)
    {
      int idx = FindStopIndexByTime(seconds);

      if (idx == -1)
      {
        double deltaSec = seconds - BaseBPM.BaseOffsetSeconds;
        return deltaSec * BaseBPM.BeatsPerSecond;
      }

      var stop = BPMStops[idx];
      double deltaSeconds = seconds - stop.StartTimeSeconds;
      return stop.StartBeat.AbsoluteValue + (deltaSeconds * stop.BeatsPerSecond);
    }

    public double ToSeconds(BeatTime beatTime) => ToSeconds(beatTime.AbsoluteValue);
    public double ToMiliSeconds(double beat) => ToSeconds(beat) * 1000.0;
    public double ToMiliSeconds(BeatTime beatTime) => ToMiliSeconds(beatTime.AbsoluteValue);

    public double ToDeltaMilliSeconds(double beat1, double beat2) => (ToSeconds(beat2) - ToSeconds(beat1)) * 1000.0;
    public double ToDeltaMilliSeconds(BeatTime beat1, BeatTime beat2) => ToDeltaMilliSeconds(beat1.AbsoluteValue, beat2.AbsoluteValue);

    public double GetCurrentBPS(double seconds)
    {
      int idx = FindStopIndexByTime(seconds);
      return idx == -1 ? BaseBPM.BeatsPerSecond : BPMStops[idx].BeatsPerSecond;
    }

    /// <summary>
    /// Finds the index of the closest BPMStop at or before the given beat. Returns -1 if none.
    /// </summary>
    private int FindStopIndex(double beat)
    {
      if (BPMStops.Count == 0 || beat < BPMStops[0].StartBeat.AbsoluteValue) return -1;

      int lo = 0, hi = BPMStops.Count - 1;
      while (lo < hi)
      {
        int mid = (lo + hi + 1) / 2;
        if (BPMStops[mid].StartBeat.AbsoluteValue <= beat) lo = mid;
        else hi = mid - 1;
      }
      return lo;
    }

    /// <summary>
    /// Finds the index of the closest BPMStop at or before the given time (in seconds). Returns -1 if none.
    /// </summary>
    private int FindStopIndexByTime(double seconds)
    {
      if (BPMStops.Count == 0 || seconds < BPMStops[0].StartTimeSeconds) return -1;

      int lo = 0, hi = BPMStops.Count - 1;
      while (lo < hi)
      {
        int mid = (lo + hi + 1) / 2;
        if (BPMStops[mid].StartTimeSeconds <= seconds) lo = mid;
        else hi = mid - 1;
      }
      return lo;
    }

    public int FindAddIndex(BPMStop bPMStop)
    {
      if (BPMStops.Count == 0) return 0;

      int left = 0, right = BPMStops.Count - 1;
      while (left <= right)
      {
        int mid = left + (right - left) / 2;
        if (BPMStops[mid].StartBeat == bPMStop.StartBeat) return mid;
        if (BPMStops[mid].StartBeat < bPMStop.StartBeat) left = mid + 1;
        else right = mid - 1;
      }
      return left;
    }
  }
}
