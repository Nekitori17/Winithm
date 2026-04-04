using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Storyboard event for property interpolation.
  /// Format: / <Property> <Start> <Length> <From> <To> <Easing>
  /// </summary>
  /// 

  public struct StoryboardEvent
  {
    public string ID;
    public BeatTime StartBeat;
    public float Length;
    public AnyValue From;
    public AnyValue To;
    public EasingType Easing;
    public AnyValue EasingBezier;
  }
}
