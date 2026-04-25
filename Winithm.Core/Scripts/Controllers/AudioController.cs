using Godot;
using Winithm.Core.Common;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  [Tool]
  public class AudioController : AudioStreamPlayer
  {
    public Metronome Metronome { get; private set; }

    public AudioController(Metronome metronome)
    {
      Metronome = metronome;
    }

    private double _manualTime = 0f;

    // DSP clock reference at last sync point.
    private double _dspTimeAtPlay = 0f;
    private double _seekPositionAtPlay = 0f;

    // Chart sync offset (seconds). Positive = audio ahead of chart.
    private double _audioOffset = 0f;

    // Hardware output latency compensation to align perceived sound with game clock.
    private double _dspBufferLatency = 0f;

    /// <summary>Perceived playback position in seconds (raw + offset + latency).</summary>
    public double CurrentTime
    {
      get
      {
        double raw = Playing
          ? _seekPositionAtPlay + (AudioServer.GetTimeSinceLastMix() - _dspTimeAtPlay)
          : _manualTime;

        return raw + _audioOffset + _dspBufferLatency;
      }
      set
      {
        // Remove offsets to get raw stream position for Seek.
        double rawValue = value - _audioOffset - _dspBufferLatency;

        if (Playing)
        {
          base.Seek((float)rawValue);
          _AnchorDsp(rawValue);
        }
        else
        {
          _manualTime = rawValue;
        }
      }
    }

    public double CurrentBeat => Metronome.ToBeat(CurrentTime);

    public double CurrentTimeMs => CurrentTime * 1000d;

    /// <summary>Pauses and captures the current raw position for accurate resume.</summary>
    public void Pause()
    {
      if (Playing)
      {
        // Capture raw position before stopping.
        _manualTime = 
          _seekPositionAtPlay + (AudioServer.GetTimeSinceLastMix() - _dspTimeAtPlay);
        Stop();
      }
    }

    /// <summary>Resume from paused position with DSP anchor reset.</summary>
    public void Resume()
    {
      if (!Playing)
      {
        Play((float)_manualTime);
        _AnchorDsp(_manualTime);
      }
    }

    public void Seek(double beat) => CurrentTime = Metronome.ToSeconds(beat);

    public double GetCurrentBPS() => Metronome.GetCurrentBPS(CurrentTime);

    /// <summary>Sets the chart sync offset in seconds.</summary>
    public void SetAudioOffsetSeconds(double offset) => _audioOffset = offset;
    public double GetAudioOffsetSeconds() => _audioOffset;

    /// <summary>Calculates latency compensation from buffer settings.</summary>
    public void SetDSPBufferSize(int bufferSizeSamples) 
      => _dspBufferLatency = (double)bufferSizeSamples / AudioServer.GetMixRate();

    /// <summary>Explicitly sets latency compensation in seconds.</summary>
    public void SetDSPBufferLatencySeconds(double latencySeconds) 
      => _dspBufferLatency = latencySeconds;

    public double GetDSPBufferLatencySeconds() => _dspBufferLatency;

    // Captures DSP clock and raw position as a sync anchor.
    private void _AnchorDsp(double rawSeekPosition)
    {
      _dspTimeAtPlay = AudioServer.GetTimeSinceLastMix();
      _seekPositionAtPlay = rawSeekPosition;
    }
  }
}