using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// BPM stop defining tempo at a given time.
  /// Format: + <StartTimeSeconds> <BPM> <TimeSignature>
  /// </summary>
  public struct BPMStop
  {
    public BeatTime StartBeat;
    public float BPM;
    public int TimeSignature;
    public float FloatStartBeat;

    /// <summary>Pre-calculated execution time in seconds. Engine computes this based on BaseOffset.</summary>
    public float StartTimeSeconds;

    public BPMStop(BeatTime startBeat, float bpm, int timeSignature)
    {
      StartBeat = startBeat;
      BPM = bpm;
      TimeSignature = timeSignature;
      FloatStartBeat = startBeat.Beat + startBeat.Numerator / startBeat.Denominator;
      StartTimeSeconds = 0f;
    }

    public float BeatsPerSecond => BPM / 60f;
  }

  /// <summary>
  /// Defines the global offset for Beat 0:0/0.
  /// Used by the Engine to align the entire beat grid to the audio.
  /// </summary>
  public struct BaseBPM
  {
    public float BaseOffsetSeconds;
    public float InitialBPM;
    public int TimeSignature;

    public BaseBPM(float offsetSeconds, float bpm, int timeSignature)
    {
      BaseOffsetSeconds = offsetSeconds;
      InitialBPM = bpm;
      TimeSignature = timeSignature;
    }

    public float BeatsPerSecond => InitialBPM / 60f;
  }
}
