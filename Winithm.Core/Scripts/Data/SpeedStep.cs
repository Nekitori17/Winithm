using System.Collections.Generic;
using Winithm.Core.Common;

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

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
