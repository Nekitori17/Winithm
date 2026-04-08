using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Storyboard event for property interpolation.
  /// Format: / <Property> <Start> <Length> <From> <To> <Easing>
  /// </summary>
  /// 

  public class StoryboardEvent
  {
    public string ID;
    public BeatTime StartBeat;
    public float Length = 0;
    public AnyValue From = new AnyValue(0f);
    public AnyValue To = new AnyValue(0f);
    public EasingType Easing = EasingType.Linear;
    public AnyValue EasingBezier = new AnyValue(0f, 0f, 1f, 1f);
  }
}
