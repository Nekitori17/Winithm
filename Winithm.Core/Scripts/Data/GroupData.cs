using System.Collections.Generic;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Hierarchical transform node from [GROUPS].
  /// Format: + <ID> <initX> <initY> <initScale> <initScaleX> <initScaleY> <initRotation>
  /// </summary>
  public class GroupData : IStoryboardTarget<StoryboardProperty>
  {
    public string ID;
    public string Name;
    public string ParentGroupID;

    public float InitX = 0f;
    public float InitY = 0f;
    public float InitScaleX = 1f;
    public float InitScaleY = 1f;
    public float InitRotation = 0f;

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
