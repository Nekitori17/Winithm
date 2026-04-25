using Godot;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  /// <summary>Master clock for chart and audio. Tick() must be called manually each frame.</summary>
  public class AudioController : Node
  {
    public Metronome Metronome { get; private set; }

    private readonly AudioStreamPlayer _player = new AudioStreamPlayer();

    // Master clock in seconds.
    private double _currentTime = 0d;

    // DSP anchor for drift correction while stream is playing.
    private double _dspTimeAtPlay = 0f;
    private double _seekPositionAtPlay = 0f;

    // Positive: music leads chart. Negative: chart leads music.
    private double _audioOffset = 0f;

    private bool _isPlaying = false;
    private bool _streamStarted = false;

    public bool IsPlaying => _isPlaying;

    public double CurrentTime => _currentTime;
    public double CurrentBeat => Metronome.ToBeat(_currentTime);
    public double CurrentTimeMs => _currentTime * 1000d;

    public AudioController(Metronome metronome)
    {
      Metronome = metronome;
      AddChild(_player);
    }

    /// <summary>Updates clock; uses DSP for drift correction or delta for pre-delay.</summary>
    public void Tick(double delta)
    {
      if (!_isPlaying) return;

      if (_streamStarted && _player.Playing)
      {
        // DSP-anchored time to correct drift. (Stream + Offset = Clock)
        _currentTime = _seekPositionAtPlay
          + (AudioServer.GetTimeSinceLastMix() - _dspTimeAtPlay)
          - _audioOffset;
      }
      else
      {
        _currentTime += delta;

        // Start stream when clock reaches start point.
        if (!_streamStarted && _currentTime >= -_audioOffset)
          _StartStream();
      }
    }

    /// <summary>Resume playback from current position.</summary>
    public void Resume()
    {
      if (_isPlaying) return;
      _isPlaying = true;

      if (_currentTime + _audioOffset >= 0)
        _StartStream();
      // Tick() will start stream once clock advances enough.
    }

    /// <summary>Pause playback.</summary>
    public void Pause()
    {
      if (!_isPlaying) return;
      _isPlaying = false;
      _streamStarted = false;
      _player.Stop();
    }

    public void SeekSeconds(double seconds)
    {
      _currentTime = seconds;
      _RestartStream();
    }

    public void SeekMilliseconds(double ms) => SeekSeconds(ms / 1000d);

    public void SeekBeat(double beat) => SeekSeconds(Metronome.ToSeconds(beat));

    public void SetAudioOffsetSeconds(double offset) => _audioOffset = offset;
    public double GetAudioOffsetSeconds() => _audioOffset;

    public AudioStream GetStream() => _player.Stream;
    public void SetStream(AudioStream stream) => _player.Stream = stream;

    // Starts stream at position relative to clock (pos = clock + offset).
    private void _StartStream()
    {
      double streamPosition = _currentTime + _audioOffset;
      _player.Play((float)streamPosition);
      _dspTimeAtPlay = AudioServer.GetTimeSinceLastMix();
      _seekPositionAtPlay = streamPosition;
      _streamStarted = true;
    }

    private void _RestartStream()
    {
      _streamStarted = false;
      _player.Stop();

      // Only restart if playing — Seek does not imply Resume.
      if (_isPlaying && _currentTime + _audioOffset >= 0)
        _StartStream();
    }
  }
}