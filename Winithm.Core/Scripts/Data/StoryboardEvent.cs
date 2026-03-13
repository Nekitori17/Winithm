using Winithm.Core.Common;

namespace Winithm.Core.Data
{
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

    /// <summary>
    /// Storyboard event for property interpolation.
    /// Format: / <Property> <Start> <Length> <From> <To> <Easing>
    /// </summary>
    public struct StoryboardEvent
    {
        public StoryboardProperty Type;
        public string CustomProperty;
        public BeatTime Start;
        public BeatTime Length;
        public string FromRaw;
        public string ToRaw;
        public float FromValue;
        public float ToValue;
        public VectorValue FromVector;
        public VectorValue ToVector;
        public bool IsVectorType;
        public bool IsStringType;
        public bool IsInherited;
        public EasingType Easing;
        public VectorValue EasingBezier;

        /// <summary>End beat (Start + Length) pre-computed for runtime.</summary>
        public float EndBeat;

        public void PreCompute()
        {
            EndBeat = Start.AbsoluteValue + Length.AbsoluteValue;
        }
    }
}
