using Godot;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  [Tool]
  public class TimeController : AudioStreamPlayer
  {
    public Metronome Metronome { get; set; }

    private double _manualTime = 0f;

    public double CurrentTime
    {
      get
      {
        if (Playing) return GetPlaybackPosition();
        return _manualTime;
      }
      set
      {
        if (Playing) base.Seek((float)value);
        else _manualTime = value;
      }
    }

    public double CurrentBeat => Metronome.ToBeat(CurrentTime);

    /// <summary>Current playback time in milliseconds.</summary>
    public double CurrentTimeMs => CurrentTime * 1000d;

    /// <summary>Pause playback (keeps position).</summary>
    public void Pause()
    {
      if (Playing)
      {
        _manualTime = GetPlaybackPosition();
        Stop();
      }
    }

    /// <summary>Resume playback from current position.</summary>
    public void Resume()
    {
      if (!Playing)
      {
        Play((float)_manualTime);
      }
    }

    /// <summary>Seek to a specific beat.</summary>
    public void Seek(double beat)
    {
      double time = Metronome.ToSeconds(beat);
      CurrentTime = time;
    }

    public double GetCurrentBPS() => Metronome.GetCurrentBPS(CurrentTime);

    public void SetAudioOffset(double offset) => Metronome.AudioOffsetSeconds = offset;
  }
}