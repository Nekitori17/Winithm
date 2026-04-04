using System.Collections.Generic;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Shared color palette from [THEME_CHANNELS].
  /// Format: + <ID> <initR> <initG> <initB> <initA> <initNoteA>
  /// </summary>
  public class ThemeChannelData : IStoryboardTarget<StoryboardProperty>
  {
    public string ID;
    public string Name;
    public float InitR = 0f;
    public float InitG = 0f;
    public float InitB = 0f;
    public float InitA = 1f;
    public float InitNoteA = 1f;

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
