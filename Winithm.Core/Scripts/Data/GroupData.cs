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
    public float InitScaleX = 1f;
    public float InitScaleY = 1f;
    public float InitRotation;

    public Dictionary<StoryboardProperty, PropertyDef> PropertyRegistry { get; } = new Dictionary<StoryboardProperty, PropertyDef>()
      {
        { StoryboardProperty.X, new PropertyDef(AnyValueType.Float) },
        { StoryboardProperty.Y, new PropertyDef(AnyValueType.Float) },
        { StoryboardProperty.ScaleX, new PropertyDef(AnyValueType.Float) },
        { StoryboardProperty.ScaleY, new PropertyDef(AnyValueType.Float) },
        { StoryboardProperty.Rotation, new PropertyDef(AnyValueType.Float) },
      };

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
