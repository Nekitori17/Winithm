using Winithm.Core.Common;
using System.Collections.Generic;

namespace Winithm.Core.Data
{
  public enum NoteType
  {
    Tap,
    Hold,
    Drag,
    Focus,
    Close
  }

  public enum NoteSide
  {
    Top,
    Bottom,
    Left,
    Right
  }

  /// <summary>
  /// Hit object data: # <NoteType> <Start> <Length> <Side> <FakeType>
  /// </summary>
  public class NoteData
  {
    public NoteType Type;
    public BeatTime Start;
    public BeatTime Length;
    public NoteSide Side;
    public int FakeType;

    public float EndBeat => Start.AbsoluteValue + Length.AbsoluteValue;

    public bool IsHittable => FakeType == 0;
    public bool IsMutedGhost => FakeType == 1;
    public bool IsLoudGhost => FakeType == 2;

    public static NoteType ParseNoteType(string text)
    {
      switch (text.Trim())
      {
        case "Tap": return NoteType.Tap;
        case "Hold": return NoteType.Hold;
        case "Drag": return NoteType.Drag;
        case "Focus": return NoteType.Focus;
        case "Close": return NoteType.Close;
        default:
          Godot.GD.PushWarning($"[WinithmParser] Unknown note type: '{text}', defaulting to Tap.");
          return NoteType.Tap;
      }
    }

    public static NoteSide ParseSide(string text)
    {
      switch (text.Trim())
      {
        case "Top": return NoteSide.Top;
        case "Bottom": return NoteSide.Bottom;
        case "Left": return NoteSide.Left;
        case "Right": return NoteSide.Right;
        default:
          Godot.GD.PushWarning($"[WinithmParser] Unknown side: '{text}', defaulting to Bottom.");
          return NoteSide.Bottom;
      }
    }
  }
}
