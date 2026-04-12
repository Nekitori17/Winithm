using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Shared utility methods for parsing and generating Winithm file formats.
  /// </summary>
  public static class ParserUtils
  {
    public static readonly CultureInfo INV = CultureInfo.InvariantCulture;

    public static bool TryParseProperty(string line, string key, out string value)
    {
      value = "";
      if (!line.StartsWith(key)) return false;
      value = line.Substring(key.Length).Trim();
      return true;
    }

    public static bool TryParseFloat(string text, out float result)
    {
      return float.TryParse(text.Trim(), NumberStyles.Float, INV, out result);
    }

    public static float ParseFloat(string text)
    {
      return float.Parse(text.Trim(), NumberStyles.Float, INV);
    }

    public static bool TryParseBool(string text, out bool result)
    {
      return bool.TryParse(text.Trim(), out result);
    }

    public static bool ParseBool(string text)
    {
      return bool.Parse(text.Trim());
    }

    /// <summary>
    /// Parses "0"/"1" integer-style boolean values.
    /// Used for flags in .wnc format where bools are stored as 0 or 1.
    /// </summary>
    public static bool TryParseIntBool(string text, out bool result)
    { 
      if (text.Contains(".") || int.TryParse(text, out int _)) {
        result = false;
        return false;
      };

      result = text.Trim() == "1";
      return true;
    }

    public static bool ParseIntBool(string text)
    {
      return text.Trim() == "1";
    }

    public static bool IsNumeric(string text)
    {
      return float.TryParse(text.Trim(), NumberStyles.Float, INV, out _);
    }

    public static string FormatFloat(float value)
    {
      string s = value.ToString("G7", INV);

      if (!s.Contains(".") && !s.Contains("E") && !s.Contains("e"))
      {
        return s + ".0";
      }
      return s;
    }

    public static string FormatIntBool(bool value)
    {
      return value ? "1" : "0";
    }

    public static string FormatIntBool(int value)
    {
      return value != 0 ? "1" : "0";
    }

    public static string FormatIntBool(float value)
    {
      return value != 0 ? "1" : "0";
    }

    public static StoryboardProperty ParseStoryboardProperty(string prop)
    {
      switch (prop)
      {
        case "Move_X": return StoryboardProperty.X;
        case "Move_Y": return StoryboardProperty.Y;
        case "Scale": return StoryboardProperty.Scale;
        case "Scale_X": return StoryboardProperty.ScaleX;
        case "Scale_Y": return StoryboardProperty.ScaleY;
        case "Rotation": return StoryboardProperty.Rotation;
        case "Color_R": return StoryboardProperty.ColorR;
        case "Color_G": return StoryboardProperty.ColorG;
        case "Color_B": return StoryboardProperty.ColorB;
        case "Color_A": return StoryboardProperty.ColorA;
        case "Note_A": return StoryboardProperty.NoteA;
        case "Title": return StoryboardProperty.Title;
        case "Speed": return StoryboardProperty.Speed;
        default: return StoryboardProperty.Custom;
      }
    }

    public static string FormatStoryboardProperty(StoryboardProperty type, string customProperty)
    {
      if (type == StoryboardProperty.Custom) return customProperty;
      switch (type)
      {
        case StoryboardProperty.X: return "Move_X";
        case StoryboardProperty.Y: return "Move_Y";
        case StoryboardProperty.Scale: return "Scale";
        case StoryboardProperty.ScaleX: return "Scale_X";
        case StoryboardProperty.ScaleY: return "Scale_Y";
        case StoryboardProperty.Rotation: return "Rotation";
        case StoryboardProperty.ColorR: return "Color_R";
        case StoryboardProperty.ColorG: return "Color_G";
        case StoryboardProperty.ColorB: return "Color_B";
        case StoryboardProperty.ColorA: return "Color_A";
        case StoryboardProperty.NoteA: return "Note_A";
        case StoryboardProperty.Title: return "Title";
        case StoryboardProperty.Speed: return "Speed";
        default: return "";
      }
    }

    /// <summary>
    /// Parse: / ID Property Start Length From To EasingOrBezier
    /// </summary>
    public static StoryboardEvent ParseStoryboardEvent(string trimmed, out StoryboardProperty type, out string rawPropertyName)
    {
      var evt = new StoryboardEvent();
      type = StoryboardProperty.Custom;
      rawPropertyName = "";
      List<string> parts = new List<string>();
      bool inQuotes = false;
      int tokenStart = 2; // skip "/ "

      for (int i = 2; i < trimmed.Length; i++)
      {
        char c = trimmed[i];
        if (c == '\"')
        {
          inQuotes = !inQuotes;
        }
        else if (c == ' ' && !inQuotes)
        {
          if (i > tokenStart)
            parts.Add(trimmed.Substring(tokenStart, i - tokenStart).Trim('\"'));
          tokenStart = i + 1;
        }
      }
      if (tokenStart < trimmed.Length)
      {
        string finalPart = trimmed.Substring(tokenStart).Trim();
        if (finalPart.Length > 0)
          parts.Add(finalPart.Trim('\"'));
      }

      if (parts.Count >= 1)
      {
        evt.ID = parts[0];
      }

      if (parts.Count >= 2)
      {
        rawPropertyName = parts[1];
        type = ParseStoryboardProperty(rawPropertyName);
      }

      if (parts.Count >= 3) evt.StartBeat = BeatTime.Parse(parts[2]);
      if (parts.Count >= 4) evt.Length = TryParseFloat(parts[3], out float length) ? length : 0;

      if (parts.Count >= 5)
        evt.From = AnyValue.Parse(parts[4]);

      if (parts.Count >= 6)
        evt.To = AnyValue.Parse(parts[5]);

      if (parts.Count >= 7)
      {
        if (parts[6].Contains("|"))
        {
          evt.Easing = EasingType.Bezier;
          evt.EasingBezier = AnyValue.Parse(parts[6]);
        }
        else
        {
          evt.Easing = EasingFunctions.ParseEasing(parts[6]);
        }
      }

      return evt;
    }

    /// <summary>Generate a storyboard event line: / Property Start Length From To Easing</summary>
    public static string GenerateStoryboardEventLine(StoryboardEvent evt, StoryboardProperty type, string customProperty = null, string indent = "  ")
    {
      string easingStr = evt.Easing == EasingType.Bezier ? evt.EasingBezier.ToString() : evt.Easing.ToString();
      string propStr = FormatStoryboardProperty(type, customProperty);

      return $"{indent}/ {evt.ID} {propStr} {evt.StartBeat} {evt.Length} {evt.From} {evt.To} {easingStr}";
    }

    public static void ResolveInheritance(ChartData data)
    {
      foreach (var component in data.Components)
        ResolveEventDict(component.StoryboardEvents);
      foreach (var theme in data.ThemeChannels)
        ResolveEventDict(theme.StoryboardEvents);
      foreach (var group in data.Groups)
        ResolveEventDict(group.StoryboardEvents);
      foreach (var overlay in data.Overlays)
        ResolveEventDict(overlay.StoryboardEvents);

      foreach (var window in data.Windows)
      {
        ResolveEventDict(window.StoryboardEvents);
        foreach (var step in window.SpeedSteps)
          ResolveEventDict(step.StoryboardEvents);
      }
    }

    /// <summary>Resolve inheritance and sort each list in a storyboard event dictionary.</summary>
    public static void ResolveEventDict<TKey>(Dictionary<TKey, List<StoryboardEvent>> dict)
    {
      if (dict == null) return;
      foreach (var kvp in dict)
        ResolveEventList(kvp.Value);
    }

    /// <summary>
    /// Resolves inherited ("-") From values within a single per-property event list,
    /// then sorts by start beat for binary search at runtime.
    /// </summary>
    public static void ResolveEventList(List<StoryboardEvent> events)
    {
      if (events.Count == 0) return;

      // Stable sort by start beat to ensure chronological order for resolution
      var sorted = System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderBy(events, e => e.StartBeat.AbsoluteValue));
      events.Clear();
      events.AddRange(sorted);

      for (int i = 1; i < events.Count; i++)
      {
        var evt = events[i];

        if (evt.From.Type == AnyValueType.Inherited)
        {
          var prev = events[i - 1];
          float currentBeat = evt.StartBeat.AbsoluteValue;

          if (currentBeat >= prev.StartBeat.AbsoluteValue + prev.Length)
          {
            // The previous event finished before this one started. Inherit its final value.
            evt.From = prev.To;
          }
          else
          {
            // This event starts WHILE the previous one is still interpolating. 
            // Calculate the exact value at this moment.
            float startBeat = prev.StartBeat.AbsoluteValue;
            float t = prev.Length > 0f ? (currentBeat - startBeat) / prev.Length : 1f;
            
            t = prev.Easing == EasingType.Bezier
              ? EasingFunctions.EvaluateBezier(prev.EasingBezier, t)
              : EasingFunctions.Evaluate(prev.Easing, t);
              
            evt.From = AnyValue.Lerp(prev.From, prev.To, t);
          }
          events[i] = evt;
        }
      }
    }

    /// <summary>Validates and adds an event to the target's dictionary, instantiating it lazily if needed.</summary>
    public static void AddEventToTarget<TKey>(IStoryboardTarget<TKey> target, TKey key, StoryboardEvent evt)
    {

      if (target.StoryboardEvents == null)
      {
        target.StoryboardEvents = new Dictionary<TKey, List<StoryboardEvent>>();
      }

      if (!target.StoryboardEvents.TryGetValue(key, out var list))
      {
        list = new List<StoryboardEvent>();
        target.StoryboardEvents[key] = list;
      }

      list.Add(evt);
    }
  }
}
