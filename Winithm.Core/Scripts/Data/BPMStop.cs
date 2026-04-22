using System;
using Winithm.Core.Common;
using Winithm.Core.Interfaces;
using Winithm.Core.Managers;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Represents a tempo change at a specific beat.
  /// </summary>
  public class BPMStop : IDeepCloneable<BPMStop>
  {
    public event Action<BPMStop> OnStartBeatChanged;
    public event Action<BPMStop> OnInvalidate;
    public event Action<BPMStop> OnUpdated;

    private BeatTime _startBeat;
    public BeatTime StartBeat { get => _startBeat; set { if (_startBeat != value) { _startBeat = value; OnStartBeatChanged?.Invoke(this); OnInvalidate?.Invoke(this); } } }

    private float _bpm;
    public float BPM { get => _bpm; set { if (_bpm != value) { _bpm = value; OnInvalidate?.Invoke(this); } } }

    private int _timeSignature;
    public int TimeSignature { get => _timeSignature; set { if (_timeSignature != value) { _timeSignature = value; OnUpdated?.Invoke(this); } } }

    public double StartTimeSeconds;

    public float BeatsPerSecond => BPM / 60f;

    public BPMStop(BeatTime startBeat, float bpm, int timeSignature)
    {
      _startBeat = startBeat;
      _bpm = bpm;
      _timeSignature = timeSignature;
      StartTimeSeconds = 0.0;
    }

    public static readonly BPMStop NaN = new BPMStop(BeatTime.NaN, 0, 0);
    public static readonly BPMStop Max = new BPMStop(BeatTime.Max, 0, 0);

    public BPMStop DeepClone(ObjectFactory objectFactory, BeatTime? offset)
    {
      return new BPMStop(_startBeat + (offset ?? BeatTime.Zero), _bpm, _timeSignature)
      {
        StartTimeSeconds = StartTimeSeconds
      };
    }

    public static BPMStop Parse(string text)
    {
      var current = new BPMStop(BeatTime.Zero, 0, 0);

      var parts = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

      if (parts.Length >= 1) current.StartBeat =
        BeatTime.TryParse(parts[0], out BeatTime startBeat) ? startBeat : BeatTime.Zero;
      if (parts.Length >= 2) current.BPM =
        ParserUtils.TryParseFloat(parts[1], out float bpm) ? bpm : 120f;
      if (parts.Length >= 3) current.TimeSignature =
        int.TryParse(parts[2], out int signature) ? signature : 4;

      return current;
    }

    public override string ToString() => $"{StartBeat} {BPM} {TimeSignature}";
  }

  /// <summary>
  /// The global timing foundation for the beat grid.
  /// </summary>
  public class BaseBPM
  {
    public event Action<BaseBPM> OnInvalidate;
    public event Action<BaseBPM> OnUpdated;

    private double _baseOffsetSeconds;
    public double BaseOffsetSeconds { get => _baseOffsetSeconds; set { if (_baseOffsetSeconds != value) { _baseOffsetSeconds = value; OnInvalidate?.Invoke(this); } } }

    private float _initialBPM;
    public float InitialBPM { get => _initialBPM; set { if (_initialBPM != value) { _initialBPM = value; OnInvalidate?.Invoke(this); } } }

    private int _timeSignature;
    public int TimeSignature { get => _timeSignature; set { if (_timeSignature != value) { _timeSignature = value; OnUpdated?.Invoke(this); } } }

    public float BeatsPerSecond => InitialBPM / 60f;

    public BaseBPM(double offsetSeconds, float bpm, int timeSignature)
    {
      _baseOffsetSeconds = offsetSeconds;
      _initialBPM = bpm;
      _timeSignature = timeSignature;
    }

    public static readonly BaseBPM NaN = new BaseBPM(0, 0, 0);

    public static BaseBPM Parse(string text)
    {
      var current = new BaseBPM(0, 0, 0);

      var parts = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

      if (parts.Length >= 1) current.BaseOffsetSeconds =
        ParserUtils.TryParseDouble(parts[0], out double offset) ? offset : 0.0;
      if (parts.Length >= 2) current.InitialBPM =
        ParserUtils.TryParseFloat(parts[1], out float bpm) ? bpm : 0f;
      if (parts.Length >= 3) current.TimeSignature =
        int.TryParse(parts[2], out int signature) ? signature : 0;

      return current;
    }

    public override string ToString() => $"{BaseOffsetSeconds} {InitialBPM} {TimeSignature}";
  }
}
