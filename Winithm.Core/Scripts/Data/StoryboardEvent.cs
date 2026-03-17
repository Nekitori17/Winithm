using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Storyboard event for property interpolation.
  /// Format: / <Property> <Start> <Length> <From> <To> <Easing>
  /// </summary>
  /// 

  public enum StoryboardProperty
  {
    Custom = 0,
    X,
    Y,
    Scale,
    ScaleX,
    ScaleY,
    Rotation,
    ColorR,
    ColorG,
    ColorB,
    ColorA,
    NoteA,
    Title,
    Speed
  }

  public enum StoryboardValueType
  {
    Int,
    Float,
    Vector,
    String,
  }

  public struct StoryboardEvent
  {
    public BeatTime Start;
    public BeatTime Length;
    public AnyValue From;
    public AnyValue To;
    public EasingType Easing;
    public AnyValue EasingBezier;

    /// <summary>End beat (Start + Length) pre-computed for runtime.</summary>
    public float EndBeat;

    public void PreCompute()
    {
      EndBeat = Start.AbsoluteValue + Length.AbsoluteValue;
    }
  }
}
