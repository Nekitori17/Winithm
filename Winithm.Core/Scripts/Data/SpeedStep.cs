using Winithm.Core.Common;
using System.Collections.Generic;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Speed step defining scrolling velocity at a given beat.
  /// Format: | <BeatTime: Start> <Float: Multiplier>
  /// Also defines the Window's lifecycle boundaries.
  /// </summary>
  public class SpeedStep : IStoryboardTarget<StoryboardProperty>
  {
    public BeatTime Start;
    public float Multiplier;

    public Dictionary<StoryboardProperty, PropertyDef> PropertyRegistry { get; } = new Dictionary<StoryboardProperty, PropertyDef>()
    {
      { StoryboardProperty.Speed, new PropertyDef(AnyValueType.Float) }
    };

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
