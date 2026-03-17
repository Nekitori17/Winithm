using System.Collections.Generic;
using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Shared color palette from [THEME_CHANNELS].
  /// Format: + <ID> <initR> <initG> <initB> <initA> <initNoteA>
  /// </summary>
  public class ThemeChannelData : IStoryboardTarget<StoryboardProperty>
  {
    public string ID = "";
    public string Name = "";
    public float InitR;
    public float InitG;
    public float InitB;
    public float InitA = 1f;
    public float InitNoteA = 1f;

    public Dictionary<StoryboardProperty, PropertyDef> PropertyRegistry { get; } = new Dictionary<StoryboardProperty, PropertyDef>()
    {
      { StoryboardProperty.ColorR, new PropertyDef(AnyValueType.Float) },
      { StoryboardProperty.ColorG, new PropertyDef(AnyValueType.Float) },
      { StoryboardProperty.ColorB, new PropertyDef(AnyValueType.Float) },
      { StoryboardProperty.ColorA, new PropertyDef(AnyValueType.Float) },
      { StoryboardProperty.NoteA, new PropertyDef(AnyValueType.Float) },
    };

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
