using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Interfaces;
using Winithm.Core.Logic;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Rendering container (Lane) from [WINDOWS].
  /// Format: + <ID> <initX> <initY> <initScale> <initScaleX> <initScaleY>
  ///           <initR> <initG> <initB> <initA> <initNoteA>
  /// </summary>
  public class WindowData : IStoryboardTarget<StoryboardProperty>
  {
    public string ID;
    public string Name;
    public string Title;
    public int Layer;
    public Vector2 Anchor;
    public string GroupID;
    public string ThemeChannelID;

    // Window Flags
    public bool Borderless = false;
    public bool UnFocus = false;

    // Transform init values
    public float InitX = 640f;
    public float InitY = 360f;
    public float InitScaleX = 300f;
    public float InitScaleY = 500f;

    // Color init values
    public float InitR = 0f;
    public float InitG = 0f;
    public float InitB = 0f;
    public float InitA = 1f;
    public float InitNoteA = 1f;

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }

    public List<SpeedStep> SpeedSteps = new List<SpeedStep>();
    public Dictionary<NoteSide, List<NoteData>> Notes = new Dictionary<NoteSide, List<NoteData>>();

    /// <summary>Window spawn beat (first SpeedStep's start)</summary>
    public BeatTime StartBeat;

    /// <summary>Window despawn beat (last Close note's start, or last SpeedStep's start)</summary>
    public BeatTime EndBeat;

    /// <summary>Whether the window will be unresponsive at end beat, triggerring a "Not Responding" visual state.</summary>
    public bool Unresponsive = false;
    /// <summary>Whether the window is currently focusable, preventing player interaction and capable of processing player input.</summary>
    public bool Focusable = false;

    /// <summary>The beat when this window becomes focusable.</summary>
    public float FocusableStartBeat = float.MaxValue;

    /// <summary>The beat when this window becomes unresponsive.</summary>
    public float FocusableEndBeat = float.MaxValue;

    // --------- Pre-computed values ---------

    /// <summary>The beat when this window finishes start animation.</summary>
    public float EndBeatStartIn = float.MaxValue;
    /// <summary>The beat when this window finishes close animation.</summary>
    public float EndBeatEndOut = float.MaxValue;

    /// <summary>
    /// The beat when this window starting close animation.
    /// Compute and Re-compute EndBeatEndOutAnimation when window is unresponsive.
    /// </summary>
    public float StartBeatEndOut = float.MaxValue;

    /// <summary>The beat when the Unresponsive overlay reaches 100% opacity.</summary>
    public float EndBeatUnresponsive = float.MaxValue;


    /// <summary>MaxEndBeats[side][i] = max EndBeat among notes[0..i]. Used for backward cursor sync.</summary>
    public Dictionary<NoteSide, float[]> MaxEndBeats = new Dictionary<NoteSide, float[]>();

    /// <summary>
    /// Pre-computes window lifecycle boundaries (StartBeat, EndBeat).
    /// Should be called after all Notes and SpeedSteps are populated.
    /// </summary>
    public void PreCompute()
    {
      StartBeat = SpeedSteps.Count > 0 ? SpeedSteps[0].StartBeat : BeatTime.Zero;

      if (SpeedSteps.Count < 2)
      {
        EndBeat = StartBeat;
        return;
      }

      EndBeat = SpeedSteps[SpeedSteps.Count - 1].StartBeat;

      // Find the last "Close" note to determine the end of the window's life
      foreach (var notes in Notes.Values)
      {
        foreach (NoteData note in notes)
        {
          if (note.Type == NoteType.Close && note.IsHittable)
          {
            if (note.StartBeat < EndBeat)
            {
              EndBeat = note.StartBeat;
            }
            break;
          }
        }
      }

      // Precompute running-max EndBeat per side for efficient backward cursor sync
      foreach (var sideNotes in Notes)
      {
        var list = sideNotes.Value;
        float[] maxEnds = new float[list.Count];
        float runningMax = float.MinValue;
        for (int i = 0; i < list.Count; i++)
        {
          runningMax = Math.Max(
            runningMax,
            list[i].StartBeat.AbsoluteValue + list[i].Length
          );
          maxEnds[i] = runningMax;
        }
        MaxEndBeats[sideNotes.Key] = maxEnds;
      }
    }

    /// <summary>
    /// Pre-computes animation-related values.
    /// Call when TimeManager.Instance.IsReady
    /// </summary>
    public void PreComputeAnimation(Metronome metronome)
    {
      float startBeatInSecs = metronome.ToSeconds(StartBeat);
      float endBeatInSecs = metronome.ToSeconds(EndBeat);

      EndBeatStartIn = metronome.ToBeat(startBeatInSecs + 0.2f);
      EndBeatEndOut = metronome.ToBeat(endBeatInSecs + 0.2f);
    }

    /// <summary>
    /// Computes animation-related values when the window is unresponsive.
    /// Call when TimeManager.Instance.IsReady
    /// </summary>
    public void ComputeAnimationWhenUnresponsive(Metronome metronome)
    {
      float endBeatInSecs = metronome.ToSeconds(EndBeat);

      EndBeatUnresponsive = metronome.ToBeat(endBeatInSecs + 0.2f);
      StartBeatEndOut = metronome.ToBeat(endBeatInSecs + 1f);
      EndBeatEndOut = metronome.ToBeat(endBeatInSecs + 1.2f);
    }
  }
}
