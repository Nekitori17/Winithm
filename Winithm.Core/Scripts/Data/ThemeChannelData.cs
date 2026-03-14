using System.Collections.Generic;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Shared color palette from [THEME_CHANNELS].
  /// Format: + <ID> <initR> <initG> <initB> <initA> <initNoteA>
  /// </summary>
  public class ThemeChannelData
  {
    public string ID = "";
    public string Name = "";
    public float InitR;
    public float InitG;
    public float InitB;
    public float InitA = 1f;
    public float InitNoteA = 1f;

    public List<StoryboardEvent> Events = new List<StoryboardEvent>();
  }
}
