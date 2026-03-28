using Godot;
using Winithm.Core.Logic;

namespace Winithm.Core.Managers
{
  [Tool]
  public class TimeManager : AudioStreamPlayer
  {
    public Metronome Metronome { get; set; }

    private float _manualTime = 0f;

    public float CurrentTime
    {
      get
      {
        if (Playing) return GetPlaybackPosition();
        return _manualTime;
      }
      set
      {
        if (Playing) base.Seek(value);
        else _manualTime = value;
      }
    }

    public float CurrentBeat => Metronome.ToBeat(CurrentTime);

    /// <summary>Current playback time in milliseconds.</summary>
    public float CurrentTimeMs => CurrentTime * 1000f;

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
        Play(_manualTime);
      }
    }

    /// <summary>Seek to a specific beat (sets CurrentTime accordingly).</summary>
    public new void Seek(float beat)
    {
      // Reverse: find time from beat
      float time = Metronome.ToSeconds(beat);
      CurrentTime = time;
    }

  }
}