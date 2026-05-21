using Godot;
using System;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  /// <summary>
  /// Master clock for chart and audio synchronization.
  /// Tick() must be called once per frame by the owner.
  /// </summary>
  public class AudioController : Node
  {
    public Metronome Metronome { get; private set; }

    private readonly AudioStreamPlayer _player = new AudioStreamPlayer();

    // Master clock in seconds. May be negative during pre-roll.
    private double _clock = 0d;

    // Positive: audio plays ahead of chart. Negative: chart leads audio.
    private double _audioOffset = 0d;

    // DSP time captured at the moment the stream started, for latency correction.
    private double _dspTimeAtStreamStart = 0d;

    private bool _isPlaying = false;
    private bool _streamStarted = false;

    // ── Public state ────────────────────────────────────────────────────────────

    public bool IsPlaying => _isPlaying;

    public double CurrentTime => _clock;
    public double CurrentTimeMs => _clock * 1000d;
    public double CurrentBeat => Metronome.ToBeat(_clock);

    public double Length => _player.Stream != null ? (double)_player.Stream.GetLength() : 0d;

    // ── Initialisation ──────────────────────────────────────────────────────────

    public void Initialize(Metronome metronome)
    {
      Metronome = metronome;
      AddChild(_player);
    }

    // ── Clock update ────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances the master clock by one frame.
    /// While the audio stream is running, the clock is anchored to the DSP
    /// position to prevent drift. During pre-roll (clock &lt; 0) or after the
    /// stream has not yet started, engine delta is used instead.
    /// </summary>
    public void Tick(double delta)
    {
      if (!_isPlaying) return;

      if (_streamStarted && _player.Playing)
      {
        // DSP-corrected position: subtract output latency so the clock reflects
        // what the listener actually hears right now.
        double dspPosition = _player.GetPlaybackPosition()
          + AudioServer.GetTimeSinceLastMix()
          - AudioServer.GetOutputLatency();

        // Convert stream position back to chart time.
        double dspClock = dspPosition - _audioOffset;

        // Godot's mix-chunk scheduler can emit the same position for multiple
        // frames or rarely step backwards. Enforce monotonic advancement:
        // accept the DSP value only when it is strictly ahead of where we are.
        if (dspClock > _clock)
          _clock = dspClock;
        else
          _clock += delta; // Keep time moving smoothly between DSP updates.
      }
      else
      {
        _clock += delta;

        // Start the stream once the clock reaches the audio start point.
        if (!_streamStarted && _clock >= -_audioOffset)
          _StartStream();
      }

      _ClampClock();
    }

    // ── Playback control ────────────────────────────────────────────────────────

    /// <summary>Resume playback from the current clock position.</summary>
    public void Resume()
    {
      if (_isPlaying) return;
      _isPlaying = true;

      // Start the stream immediately if the clock is already past the pre-roll;
      // otherwise Tick() will trigger it once the clock advances enough.
      if (_clock + _audioOffset >= 0d)
        _StartStream();
    }

    /// <summary>Pause playback and stop the audio stream.</summary>
    public void Pause()
    {
      if (!_isPlaying) return;
      _isPlaying = false;
      _streamStarted = false;
      _player.Stop();
    }

    // ── Seeking ─────────────────────────────────────────────────────────────────

    public void SeekSeconds(double seconds)
    {
      _clock = seconds;
      _ClampClock();
      _RestartStream();
    }

    public void SeekMilliseconds(double ms) => SeekSeconds(ms / 1000d);
    public void SeekBeat(double beat) => SeekSeconds(Metronome.ToSeconds(beat));

    // ── Clock nudge (used by rewind animation while paused) ─────────────────────

    /// <summary>
    /// Shifts the clock by <paramref name="deltaSecs"/> without resuming playback.
    /// Clamps the result to the valid playable range.
    /// </summary>
    public void AdjustTime(double deltaSecs)
    {
      _clock += deltaSecs;
      _ClampClock();
    }

    // ── Audio offset ────────────────────────────────────────────────────────────

    public void SetAudioOffsetSeconds(double offset) => _audioOffset = offset;
    public double GetAudioOffsetSeconds() => _audioOffset;

    // ── Stream access ───────────────────────────────────────────────────────────

    public AudioStream GetStream() => _player.Stream;
    public void SetStream(AudioStream stream) => _player.Stream = stream;

    // ── Private helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Clamps the clock so it never goes below 0 or beyond the playable end of
    /// the track. The upper bound accounts for audio offset direction:
    ///   • positive offset → audio leads, effective end is Length - offset
    ///   • negative offset → audio lags, effective end is Length - offset (still correct)
    /// When there is no stream loaded the upper bound is unconstrained.
    /// </summary>
    private void _ClampClock()
    {
      if (_clock < 0d) _clock = 0d;

      double streamLength = Length;
      if (streamLength <= 0d) return;

      // Clamp to whichever ends later: the chart or the audio stream.
      double maxTime = Math.Max(streamLength, streamLength - _audioOffset);
      if (_clock > maxTime) _clock = maxTime;
    }

    /// <summary>Starts the audio stream at the position that matches the current clock.</summary>
    private void _StartStream()
    {
      double streamPosition = _clock + _audioOffset;

      if (streamPosition > Length) return;

      _player.Play((float)streamPosition);
      _dspTimeAtStreamStart = AudioServer.GetTimeSinceLastMix();
      _streamStarted = true;
    }

    private void _RestartStream()
    {
      _streamStarted = false;
      _player.Stop();

      // Seek does not imply resume — only restart the stream if already playing.
      if (_isPlaying && _clock + _audioOffset >= 0d)
        _StartStream();
    }
  }
}