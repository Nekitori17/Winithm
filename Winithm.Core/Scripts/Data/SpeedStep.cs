using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Speed step defining scrolling velocity at a given beat.
  /// Format: | <BeatTime: Start> <Float: Multiplier>
  /// Also defines the Window's lifecycle boundaries.
  /// </summary>
  public class SpeedStep : IStoryboardTarget<StoryboardProperty>
  {
    public string ID;
    public BeatTime StartBeat;
    public float Multiplier = 1f;

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }
  }
}
