using System.Collections.Generic;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Hierarchical transform node from [GROUPS].
  /// Format: + <ID> <initX> <initY> <initScale> <initScaleX> <initScaleY> <initRotation>
  /// </summary>
  public class GroupData
  {
    public string ID = "";
    public string Name = "";
    public string ParentGroupID = "";

    public float InitX;
    public float InitY;
    public float InitScaleX = 1f;
    public float InitScaleY = 1f;
    public float InitRotation;

    public List<StoryboardEvent> Events = new List<StoryboardEvent>();
  }
}
