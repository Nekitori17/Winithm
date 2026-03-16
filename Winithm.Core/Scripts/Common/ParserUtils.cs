using System;
using System.Collections.Generic;
using System.Globalization;
using Winithm.Core.Data;

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

    public static float ParseFloat(string text)
    {
      float.TryParse(text.Trim(), NumberStyles.Float, INV, out float result);
      return result;
    }

    public static bool ParseBool(string text)
    {
      bool.TryParse(text.Trim(), out bool result);
      return result;
    }

    public static bool IsNumeric(string text)
    {
      return float.TryParse(text.Trim(), NumberStyles.Float, INV, out _);
    }

    public static string FormatFloat(float value)
    {
      return value.ToString(INV);
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
    /// Parse: / Property Start Length From To Easing
    /// </summary>
    public static StoryboardEvent ParseStoryboardEvent(string trimmed)
    {
      var evt = new StoryboardEvent();
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
        evt.Type = ParseStoryboardProperty(parts[0]);
        if (evt.Type == StoryboardProperty.Custom)
          evt.CustomProperty = parts[0];
      }

      if (parts.Count >= 2) evt.Start = BeatTime.Parse(parts[1]);
      if (parts.Count >= 3) evt.Length = BeatTime.Parse(parts[2]);

      if (parts.Count >= 4)
      {
        evt.FromRaw = parts[3];
        if (parts[3] == "-")
        {
          evt.IsInherited = true;
        }
        else if (VectorValue.IsVectorFormat(parts[3]))
        {
          evt.IsVectorType = true;
          evt.FromVector = VectorValue.Parse(parts[3]);
        }
        else if (IsNumeric(parts[3]))
        {
          evt.FromValue = ParseFloat(parts[3]);
        }
        else
        {
          evt.IsStringType = true;
        }
      }

      if (parts.Count >= 5)
      {
        evt.ToRaw = parts[4];
        if (VectorValue.IsVectorFormat(parts[4]))
        {
          evt.IsVectorType = true;
          evt.ToVector = VectorValue.Parse(parts[4]);
        }
        else if (IsNumeric(parts[4]))
        {
          evt.ToValue = ParseFloat(parts[4]);
        }
        else
        {
          evt.IsStringType = true;
        }
      }

      if (parts.Count >= 6)
      {
        if (VectorValue.IsVectorFormat(parts[5]))
        {
          evt.Easing = EasingType.Bezier;
          evt.EasingBezier = VectorValue.Parse(parts[5]);
        }
        else
        {
          evt.Easing = EasingFunctions.ParseEasing(parts[5]);
        }
      }

      evt.PreCompute();
      return evt;
    }

    /// <summary>Generate a storyboard event line: / Property Start Length From To Easing</summary>
    public static string GenerateStoryboardEventLine(StoryboardEvent evt, string indent = "  ")
    {
      string from, to;

      if (evt.IsInherited)
      {
        from = "-";
      }
      else if (evt.IsVectorType)
      {
        from = evt.FromVector.ToString();
      }
      else
      {
        from = evt.FromRaw ?? FormatFloat(evt.FromValue);
      }

      if (evt.IsVectorType)
      {
        to = evt.ToVector.ToString();
      }
      else
      {
        to = evt.ToRaw ?? FormatFloat(evt.ToValue);
      }

      if (evt.IsStringType)
      {
        if (from != "-" && (from.Contains(" ") || string.IsNullOrEmpty(from))) from = $"\"{from}\"";
        if (to.Contains(" ") || string.IsNullOrEmpty(to)) to = $"\"{to}\"";
      }

      string easingStr = evt.Easing == EasingType.Bezier ? evt.EasingBezier.ToString() : evt.Easing.ToString();
      string propStr = FormatStoryboardProperty(evt.Type, evt.CustomProperty);
      return $"{indent}/ {propStr} {evt.Start} {evt.Length} {from} {to} {easingStr}";
    }

    public static void PreCalculateBPMStops(List<BPMStop> stops)
    {
      if (stops.Count == 0) return;

      var first = stops[0];
      first.AbsoluteBeat = 0f;
      stops[0] = first;

      for (int i = 1; i < stops.Count; i++)
      {
        var prev = stops[i - 1];
        var curr = stops[i];
        float deltaTime = curr.StartTimeSeconds - prev.StartTimeSeconds;
        curr.AbsoluteBeat = prev.AbsoluteBeat + deltaTime * prev.BeatsPerSecond;
        stops[i] = curr;
      }
    }

    // /// <summary>
    // /// Removes notes that start before their window's StartBeat or after their window's EndBeat.
    // /// </summary>
    // public static void CullNotesOutsideWindows(ChartData data)
    // {
    //   foreach (var window in data.Windows)
    //   {
    //     float startBeat = window.StartBeat;
    //     float endBeat = window.EndBeat;
    //     window.Notes.RemoveAll(n =>
    //       n.Start.AbsoluteValue < startBeat || n.Start.AbsoluteValue > endBeat
    //     );
    //   }
    // }

    public static void ResolveInheritance(ChartData data)
    {
      foreach (var component in data.Components)
        ResolveEventList(component.Events);
      foreach (var theme in data.ThemeChannels)
        ResolveEventList(theme.Events);
      foreach (var group in data.Groups)
        ResolveEventList(group.Events);
      foreach (var overlay in data.Overlays)
        ResolveEventList(overlay.Events);

      foreach (var window in data.Windows)
      {
        ResolveEventList(window.Events);
        foreach (var step in window.SpeedSteps)
          ResolveEventList(step.Events);
        foreach (var note in window.Notes)
          ResolveEventList(note.Events);
      }
    }

    public static void ResolveEventList(List<StoryboardEvent> events)
    {
      var lastValues = new Dictionary<string, StoryboardEvent>();

      for (int i = 0; i < events.Count; i++)
      {
        var evt = events[i];
        string key = evt.Type == StoryboardProperty.Custom ? evt.CustomProperty : evt.Type.ToString();

        if (evt.IsInherited && lastValues.ContainsKey(key))
        {
          var prev = lastValues[key];
          evt.FromValue = prev.ToValue;
          evt.FromVector = prev.ToVector;
          evt.IsVectorType = prev.IsVectorType;
          evt.IsStringType = prev.IsStringType;

          // Derive FromRaw from the correct typed value to avoid type mismatch
          if (prev.IsVectorType)
            evt.FromRaw = prev.ToVector.ToString();
          else if (prev.IsStringType)
            evt.FromRaw = prev.ToRaw;
          else
            evt.FromRaw = FormatFloat(prev.ToValue);

          evt.IsInherited = false;
          events[i] = evt;
        }

        lastValues[key] = evt;
      }
    }
  }
}
