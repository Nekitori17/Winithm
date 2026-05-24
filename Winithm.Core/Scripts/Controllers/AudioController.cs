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

    // Chart time in seconds. Can be negative during pre-roll.
    private double _clock = 0d;

    private double _minClock = 0d;
    private double _maxClock = 0d;

    // Positive: audio leads chart. Negative: chart leads audio.
    private double _audioOffset = 0d;

    private bool _isPlaying = false;
    private bool _streamStarted = false;

    // ── Public state ────────────────────────────────────────────────────────────

    public bool IsPlaying => _isPlaying;

    public double CurrentTime => _clock - _minClock;
    public double CurrentTimeMs => CurrentTime * 1000d;
    public double CurrentBeat => Metronome.ToBeat(_clock);

    public double Length => _player.Stream != null ? (double)_player.Stream.GetLength() : 0d;
    public double LevelLength => Length + Math.Abs(_audioOffset);

    // ── Initialisation ──────────────────────────────────────────────────────────

    public void Initialize(Metronome metronome)
    {
      Metronome = metronome;
      AddChild(_player);
    }

    // ── Clock update ────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances the master clock one frame.
    ///
    /// • When the stream is running: clock is derived from the DSP position
    ///   (drift-free). Monotonic fallback to delta when DSP hasn't advanced.
    /// • When in pre-roll (clock &lt; -offset, offset > 0) or waiting for the
    ///   audio entry point (offset &lt; 0): clock advances by delta until the
    ///   stream entry condition is met.
    /// </summary>
    public void Tick(double delta)
    {
      if (!_isPlaying) return;

      if (_streamStarted && _player.Playing)
      {
        // DSP-corrected stream position — accounts for output latency so the
        // clock matches what the listener actually hears right now.
        double dspPosition = _player.GetPlaybackPosition()
          + AudioServer.GetTimeSinceLastMix()
          - AudioServer.GetOutputLatency();

        // Convert stream position back to chart time.
        double dspClock = dspPosition - _audioOffset;

        // Godot's mix-chunk scheduler can hold or slightly reverse the reported
        // position between frames. Enforce monotonic advancement.
        if (dspClock > _clock)
          _clock = dspClock;
        else
          _clock += delta;
      }
      else
      {
        _clock += delta;

        // stream entry condition: clock + offset >= 0  →  stream position >= 0
        if (!_streamStarted && _clock + _audioOffset >= 0d)
          _StartStream();
      }

      _ClampClock();
    }

    // ── Playback control ────────────────────────────────────────────────────────

    /// <summary>
    /// Resumes playback from the current clock position.
    /// When starting from zero with a positive offset, seeds the clock into
    /// pre-roll (-offset) so the audio stream always begins at position 0.
    /// </summary>
    public void Resume()
    {
      if (_isPlaying) return;
      _isPlaying = true;

      // Starting fresh (clock == 0) with audio-leads offset: the stream entry
      // point is in the future. Seed into pre-roll so Tick() advances toward it
      // and _StartStream() fires with streamPosition == 0.
      if (_clock == 0d && _audioOffset > 0d)
        _clock = -_audioOffset;

      // Already past the stream entry point → start immediately.
      // Still in pre-roll → Tick() will call _StartStream() when ready.
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

    /// <summary>
    /// Seeks to an absolute chart-time position and restarts the stream if playing.
    /// The caller is responsible for passing a value in the valid chart range;
    /// this method clamps it to be safe.
    /// </summary>
    public void SeekSeconds(double seconds)
    {
      _clock = seconds;
      _ClampClock();
      _RestartStream();
    }

    public void SeekMilliseconds(double ms) => SeekSeconds(ms / 1000d);
    public void SeekBeat(double beat) => SeekSeconds(Metronome.ToSeconds(beat));

    // ── Clock nudge (rewind animation while paused) ──────────────────────────────

    /// <summary>
    /// Shifts the clock by deltaSecs without resuming playback.
    /// Clamps the result to the valid chart range.
    /// </summary>
    public void AdjustTime(double deltaSecs)
    {
      _clock += deltaSecs;
      _ClampClock();
    }

    // ── Audio offset ────────────────────────────────────────────────────────────

    public void SetAudioOffsetSeconds(double offset) => _audioOffset = offset;
    public void SetAudioOffsetMS(double ms) => _audioOffset = ms / 1000d;
    public double GetAudioOffsetSeconds() => _audioOffset;
    public double GetAudioOffsetMS() => _audioOffset * 1000d;

    // ── Stream access ───────────────────────────────────────────────────────────

    public AudioStream GetStream() => _player.Stream;
    public void SetStream(AudioStream stream) => _player.Stream = stream;

    // ── Private helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Clamps _clock to the valid playable range.
    /// The level ends only when BOTH the chart and the audio have finished.
    /// </summary>
    private void _ClampClock()
    {
      _minClock = -Math.Max(0d, _audioOffset); // pre-roll floor

      if (_clock < _minClock) _clock = _minClock;

      double streamLength = Length;
      if (streamLength <= 0d) return;

      _maxClock = Math.Max(streamLength, streamLength - _audioOffset);
      if (_clock > _maxClock) _clock = _maxClock;
    }

    /// <summary>
    /// Starts the audio stream at the position matching the current clock.
    /// streamPosition = clock + offset, which equals 0 when the stream first
    /// becomes eligible (clock = -offset), and increases from there.
    /// Guards against out-of-range values in case of floating-point slippage.
    /// </summary>
    private void _StartStream()
    {
      double streamPosition = _clock + _audioOffset;

      // Clamp to valid stream range — should never be needed in normal flow.
      streamPosition = Math.Max(0d, Math.Min(streamPosition, Length));

      _player.Play((float)streamPosition);
      _streamStarted = true;
    }

    private void _RestartStream()
    {
      _streamStarted = false;
      _player.Stop();

      // Seek does not imply resume; only restart the stream if already playing
      // and the clock is past the stream entry point.
      if (_isPlaying && _clock + _audioOffset >= 0d)
        _StartStream();
    }
  }
}