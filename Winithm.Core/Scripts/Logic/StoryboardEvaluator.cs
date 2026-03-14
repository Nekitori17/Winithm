using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.Logic
{
  /// <summary>
  /// Evaluates StoryboardEvent lists at any given beat time.
  /// Returns the interpolated value (float, Color, or string) for a given property.
  /// Fully deterministic: same beat always = same output.
  /// </summary>
  public static class StoryboardEvaluator
  {
    /// <summary>
    /// Evaluate a float property from an event list at the given beat.
    /// Returns the interpolated value, or defaultValue if no events are active.
    /// </summary>
    public static float EvaluateFloat(List<StoryboardEvent> events, StoryboardProperty property, float currentBeat, float defaultValue, string customProperty = null)
    {
      float result = defaultValue;
      bool found = false;

      for (int i = 0; i < events.Count; i++)
      {
        var evt = events[i];
        if (evt.Type != property) continue;
        if (property == StoryboardProperty.Custom && evt.CustomProperty != customProperty) continue;
        if (evt.IsStringType || evt.IsVectorType) continue;

        float startBeat = evt.Start.AbsoluteValue;
        float endBeat = evt.EndBeat;

        if (currentBeat < startBeat)
        {
          // Event hasn't started yet — if we already found a completed event, keep its value
          if (!found) continue;
          break;
        }

        found = true;

        if (currentBeat >= endBeat)
        {
          // Event fully completed, snap to To value
          result = evt.ToValue;
        }
        else
        {
          // Event is active, interpolate
          float length = endBeat - startBeat;
          float t = length > 0f ? (currentBeat - startBeat) / length : 1f;
          t = evt.Easing == EasingType.Bezier ? EasingFunctions.EvaluateBezier(evt.EasingBezier, t) : EasingFunctions.Evaluate(evt.Easing, t);
          result = evt.FromValue + (evt.ToValue - evt.FromValue) * t;
        }
      }

      return result;
    }

    /// <summary>
    /// Evaluate a Vector property from an event list at the given beat.
    /// </summary>
    public static VectorValue EvaluateVector(List<StoryboardEvent> events, StoryboardProperty property, float currentBeat, VectorValue defaultValue, string customProperty = null)
    {
      VectorValue result = defaultValue;
      bool found = false;

      for (int i = 0; i < events.Count; i++)
      {
        var evt = events[i];
        if (evt.Type != property) continue;
        if (property == StoryboardProperty.Custom && evt.CustomProperty != customProperty) continue;
        if (!evt.IsVectorType) continue;

        float startBeat = evt.Start.AbsoluteValue;
        float endBeat = evt.EndBeat;

        if (currentBeat < startBeat)
        {
          if (!found) continue;
          break;
        }

        found = true;

        if (currentBeat >= endBeat)
        {
          result = evt.ToVector;
        }
        else
        {
          float length = endBeat - startBeat;
          float t = length > 0f ? (currentBeat - startBeat) / length : 1f;
          t = evt.Easing == EasingType.Bezier ? EasingFunctions.EvaluateBezier(evt.EasingBezier, t) : EasingFunctions.Evaluate(evt.Easing, t);
          result = VectorValue.Lerp(evt.FromVector, evt.ToVector, t);
        }
      }

      return result;
    }

    /// <summary>
    /// Evaluate a string property (e.g., Title).
    /// Flips from From to To when t >= 1.0.
    /// </summary>
    public static string EvaluateString(List<StoryboardEvent> events, StoryboardProperty property, float currentBeat, string defaultValue, string customProperty = null)
    {
      string result = defaultValue;

      for (int i = 0; i < events.Count; i++)
      {
        var evt = events[i];
        if (evt.Type != property) continue;
        if (property == StoryboardProperty.Custom && evt.CustomProperty != customProperty) continue;
        if (!evt.IsStringType) continue;

        float endBeat = evt.EndBeat;

        if (currentBeat >= endBeat)
          result = evt.ToRaw ?? defaultValue;
        else if (currentBeat >= evt.Start.AbsoluteValue)
          result = evt.FromRaw ?? defaultValue;
      }

      return result;
    }

    /// <summary>
    /// Batch-evaluate all float properties from an event list at the given beat,
    /// writing results into dictionaries. Avoids per-property iteration.
    /// </summary>
    public static void EvaluateAll(List<StoryboardEvent> events, float currentBeat,
        Dictionary<StoryboardProperty, float> floatResults,
        Dictionary<string, float> customResults = null)
    {
      for (int i = 0; i < events.Count; i++)
      {
        var evt = events[i];
        if (evt.IsStringType || evt.IsVectorType) continue;

        float startBeat = evt.Start.AbsoluteValue;
        float endBeat = evt.EndBeat;

        if (currentBeat < startBeat) continue;

        float value;
        if (currentBeat >= endBeat)
        {
          value = evt.ToValue;
        }
        else
        {
          float length = endBeat - startBeat;
          float t = length > 0f ? (currentBeat - startBeat) / length : 1f;
          t = evt.Easing == EasingType.Bezier ? EasingFunctions.EvaluateBezier(evt.EasingBezier, t) : EasingFunctions.Evaluate(evt.Easing, t);
          value = evt.FromValue + (evt.ToValue - evt.FromValue) * t;
        }

        if (evt.Type == StoryboardProperty.Custom)
        {
          if (customResults != null && evt.CustomProperty != null)
            customResults[evt.CustomProperty] = value;
        }
        else
        {
          floatResults[evt.Type] = value;
        }
      }
    }
  }
}
