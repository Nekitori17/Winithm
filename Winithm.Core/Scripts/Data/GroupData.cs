using System.Collections.Generic;
using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Hierarchical transform node from [GROUPS].
  /// Format: + <ID> <initX> <initY> <initScale> <initScaleX> <initScaleY> <initRotation>
  /// </summary>
  public class GroupData : IStoryboardTarget<StoryboardProperty>
  {
    public string ID = "";
    public string Name = "";
    public string ParentGroupID = "";

    public float InitX;
    public float InitY;
    public float InitScaleX;
    public float InitScaleY;
    public float InitRotation;

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
