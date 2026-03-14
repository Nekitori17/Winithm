using Winithm.Core.Common;
using System.Collections.Generic;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Speed step defining scrolling velocity at a given beat.
  /// Format: | <BeatTime: Start> <Float: Multiplier>
  /// Also defines the Window's lifecycle boundaries.
  /// </summary>
  public class SpeedStep
  {
    public BeatTime Start;
    public float Multiplier;

    /// <summary>Dynamic speed events (child storyboard events overriding this step)</summary>
    public List<StoryboardEvent> Events = new List<StoryboardEvent>();
  }
}
