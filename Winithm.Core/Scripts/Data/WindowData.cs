using System.Collections.Generic;
using Winithm.Core.Behaviors;
using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Rendering container (Lane) from [WINDOWS].
  /// Format: + <ID> <initX> <initY> <initScale> <initScaleX> <initScaleY>
  ///           <initR> <initG> <initB> <initA> <initNoteA>
  /// </summary>
  public class WindowData : IStoryboardTarget<StoryboardProperty>
  {
    public string ID = "";
    public string Name = "";
    public string Title = "";
    public int Layer;
    public float AnchorX = 0.5f;
    public float AnchorY = 0.5f;
    public string GroupID = "";
    public string ThemeChannelID = "";

    // Window Flags
    public bool Borderless = false;
    public bool UnFocus = false;

    // Transform init values
    public float InitX;
    public float InitY;
    public float InitScaleX = 1f;
    public float InitScaleY = 3f;

    // Color init values
    public float InitR;
    public float InitG;
    public float InitB;
    public float InitA = 1f;
    public float InitNoteA = 1f;

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }

    public List<SpeedStep> SpeedSteps = new List<SpeedStep>();
    public Dictionary<NoteSide, List<NoteData>> Notes = new Dictionary<NoteSide, List<NoteData>>();

    /// <summary>Window spawn beat (first SpeedStep's start)</summary>
    public float StartBeat;

    /// <summary>Window despawn beat (last Close note's start, or last SpeedStep's start)</summary>
    public float EndBeat;

    /// <summary>Cached real-time start (in seconds)</summary>
    public float StartTimeSecs;

    /// <summary>Cached real-time end (in seconds)</summary>
    public float EndTimeSecs;

    /// <summary>
    /// Pre-computes window lifecycle boundaries (StartBeat, EndBeat).
    /// Should be called after all Notes and SpeedSteps are populated.
    /// </summary>
    public void PreCompute()
    {
      StartBeat = SpeedSteps.Count > 0 ? SpeedSteps[0].Start.AbsoluteValue : 0f;
      EndBeat = StartBeat;

      if (SpeedSteps.Count < 2)
      {
        EndBeat = StartBeat;
        return;
      }

      // Find the last "Close" note to determine the end of the window's life
      foreach (var notes in Notes.Values)
      {
        foreach (NoteData note in notes)
        {
          if (note.Type == NoteType.Close)
          {
            EndBeat = note.Start.AbsoluteValue;
            return;
          }
        }
      }

      // If no Close note, use the start of the last SpeedStep
      EndBeat = SpeedSteps[SpeedSteps.Count - 1].Start.AbsoluteValue;
    }
  }
}
