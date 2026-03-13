using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Engine
{
    /// <summary>
    /// Deterministic Time ↔ Beat converter with O(log N) Binary Search.
    /// Constructed from a BPM List parsed by WNMParser.
    /// </summary>
    public class Metronome
    {
        private readonly List<BPMStop> _stops;

        public Metronome(List<BPMStop> stops)
        {
            _stops = stops;
        }

        /// <summary>Current BPM count.</summary>
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
            if (_stops.Count == 0) return 0f;

            int idx = FindStopIndex(beat);
            var stop = _stops[idx];

            float deltaBeat = beat - stop.AbsoluteBeat;
            float deltaSeconds = deltaBeat / stop.BeatsPerSecond;
            return stop.StartTimeSeconds + deltaSeconds;
        }

        /// <summary>
        /// Convert seconds → absolute beat.
        /// Uses O(log N) binary search to find the active BPM stop.
        /// </summary>
        public float ToBeat(float seconds)
        {
            if (_stops.Count == 0) return 0f;

            int idx = FindStopIndexByTime(seconds);
            var stop = _stops[idx];

            float deltaSeconds = seconds - stop.StartTimeSeconds;
            float deltaBeat = deltaSeconds * stop.BeatsPerSecond;
            return stop.AbsoluteBeat + deltaBeat;
        }

        /// <summary>
        /// Convert a BeatTime struct → seconds.
        /// </summary>
        public float ToSeconds(Common.BeatTime beatTime)
        {
            return ToSeconds(beatTime.AbsoluteValue);
        }

        // ──────────────────────────────────────────────
        //  Binary Search
        // ──────────────────────────────────────────────

        /// <summary>Find the BPM stop index that contains the given beat (by AbsoluteBeat).</summary>
        private int FindStopIndex(float beat)
        {
            int lo = 0;
            int hi = _stops.Count - 1;

            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (_stops[mid].AbsoluteBeat <= beat)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            return lo;
        }

        /// <summary>Find the BPM stop index that contains the given time (by StartTimeSeconds).</summary>
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
