using System.Collections.Generic;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Rendering container (Lane) from [WINDOWS].
  /// Format: + <ID> <initX> <initY> <initScale> <initScaleX> <initScaleY>
  ///           <initR> <initG> <initB> <initA> <initNoteA>
  /// </summary>
  public class WindowData
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

    // Children
    public List<StoryboardEvent> Events = new List<StoryboardEvent>();
    public List<SpeedStep> SpeedSteps = new List<SpeedStep>();
    public List<NoteData> Notes = new List<NoteData>();

    /// <summary>Window spawn beat (first SpeedStep's start)</summary>
    public float StartBeat => SpeedSteps.Count > 0 ? SpeedSteps[0].Start.AbsoluteValue : 0f;

    /// <summary>Window despawn beat (last SpeedStep's start)</summary>
    public float EndBeat => SpeedSteps.Count > 0 ? SpeedSteps[SpeedSteps.Count - 1].Start.AbsoluteValue : 0f;
  }
}
