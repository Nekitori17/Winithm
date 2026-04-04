using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Common;

namespace Winithm.Core.Logic
{
  /// <summary>
  /// Deterministic Time ↔ Beat converter with O(log N) Binary Search.
  /// Constructed from a BPM List parsed by WNMParser.
  /// </summary>
  public class Metronome
  {
    private readonly BaseBPM _baseBPM;
    private readonly List<BPMStop> _stops;

    public Metronome(BaseBPM baseBPM, List<BPMStop> stops)
    {
      _baseBPM = baseBPM;
      _stops = new List<BPMStop>();

      // BaseBPM becomes the master Stop at Beat 0:0/0
      var firstStop = new BPMStop(BeatTime.Zero, baseBPM.InitialBPM, baseBPM.TimeSignature)
      {
        StartTimeSeconds = baseBPM.BaseOffsetSeconds
      };
      _stops.Add(firstStop);

      if (stops != null)
      {
        foreach (var stop in stops)
        {
          // Ignore manual stops placed at Beat 0 to prevent conflicts with BaseBPM
          if (stop.StartBeat.AbsoluteValue > 0f)
            _stops.Add(stop);
        }
      }

      PreCompute();
    }

    /// <summary>Current BPM count (always at least 1 due to BaseBPM).</summary>
    public int StopCount => _stops.Count;

    /// <summary>Get BPM at a given beat.</summary>
    public float GetBPMAtBeat(float beat)
    {
      int idx = FindStopIndex(beat);
      return _stops[idx].BPM;
    }

    /// <summary>Get time signature at a given beat.</summary>
    public int GetTimeSignatureAtBeat(float beat)
    {
      int idx = FindStopIndex(beat);
      return _stops[idx].TimeSignature;
    }

    // ──────────────────────────────────────────────
    //  Core Conversions
    // ──────────────────────────────────────────────

    /// <summary>
    /// Convert absolute beat → seconds.
    /// Uses O(log N) binary search to find the active BPM stop.
    /// </summary>
    public float ToSeconds(float beat)
    {
      int idx = FindStopIndex(beat);
      var stop = _stops[idx];

      float deltaBeat = beat - stop.StartBeat.AbsoluteValue;
      float deltaSeconds = deltaBeat / stop.BeatsPerSecond;
      return stop.StartTimeSeconds + deltaSeconds;
    }

    /// <summary>
    /// Convert seconds → absolute beat.
    /// Uses O(log N) binary search to find the active BPM stop.
    /// </summary>
    public float ToBeat(float seconds)
    {
      int idx = FindStopIndexByTime(seconds);
      var stop = _stops[idx];

      float deltaSeconds = seconds - stop.StartTimeSeconds;
      float deltaBeat = deltaSeconds * stop.BeatsPerSecond;
      return stop.StartBeat.AbsoluteValue + deltaBeat;
    }

    /// <summary>
    /// Convert a BeatTime struct → seconds.
    /// </summary>
    public float ToSeconds(BeatTime beatTime)
    {
      return ToSeconds(beatTime.AbsoluteValue);
    }

    public float GetCurrentBPS(float seconds)
    {
      int idx = FindStopIndexByTime(seconds);
      return _stops[idx].BeatsPerSecond;
    }

    /// <summary>
    /// Computes StartTimeSeconds for each BPMStop sequentially.
    /// Call this after constructing the Metronome.
    /// </summary>
    public void PreCompute()
    {
      var first = _stops[0];
      first.StartTimeSeconds = _baseBPM.BaseOffsetSeconds;
      _stops[0] = first;

      for (int i = 1; i < _stops.Count; i++)
      {
        var prev = _stops[i - 1];
        var curr = _stops[i];

        float beatDiff = curr.StartBeat.AbsoluteValue - prev.StartBeat.AbsoluteValue;
        curr.StartTimeSeconds = prev.StartTimeSeconds + (beatDiff / prev.BeatsPerSecond);

        _stops[i] = curr;
      }
    }

    // ──────────────────────────────────────────────
    //  Binary Search
    // ──────────────────────────────────────────────

    /// <summary>Find the BPM stop index that contains the given absolute beat.</summary>
    private int FindStopIndex(float beat)
    {
      int lo = 0;
      int hi = _stops.Count - 1;

      while (lo < hi)
      {
        int mid = (lo + hi + 1) / 2;
        if (_stops[mid].StartBeat.AbsoluteValue <= beat)
          lo = mid;
        else
          hi = mid - 1;
      }

      return lo;
    }

    /// <summary>Find the BPM stop index that contains the given time (seconds).</summary>
    private int FindStopIndexByTime(float seconds)
    {
      int lo = 0;
      int hi = _stops.Count - 1;

      while (lo < hi)
      {
        int mid = (lo + hi + 1) / 2;
        if (_stops[mid].StartTimeSeconds <= seconds)
          lo = mid;
        else
          hi = mid - 1;
      }

      return lo;
    }
  }
}
