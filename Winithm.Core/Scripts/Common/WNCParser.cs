using System;
using System.Collections.Generic;
using System.IO;
using Winithm.Core.Data;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Parser for .wnc (Chart) files.
  /// Reads [FORMAT], [METADATA], [OVERLAYS], [COMPONENTS], [THEME_CHANNELS], [GROUPS], [WINDOWS].
  /// </summary>
  public static class WNCParser
  {
    /// <summary>Parse a .wnc chart file into the given ChartData.</summary>
    public static void Parse(string filePath, ChartData data)
    {
      string[] lines = File.ReadAllLines(filePath);
      string currentSection = "";

      ComponentData currentComponent = null;
      ThemeChannelData currentTheme = null;
      GroupData currentGroup = null;
      WindowData currentWindow = null;
      OverlayData currentOverlay = null;
      SpeedStep currentSpeedStep = null;
      NoteData currentNote = null;

      for (int i = 0; i < lines.Length; i++)
      {
        string line = lines[i].TrimEnd();
        if (string.IsNullOrWhiteSpace(line)) continue;

        if (line.StartsWith("[") && line.EndsWith("]"))
        {
          currentSection = line.Substring(1, line.Length - 2);
          currentComponent = null;
          currentTheme = null;
          currentGroup = null;
          currentWindow = null;
          currentOverlay = null;
          currentSpeedStep = null;
          currentNote = null;
          continue;
        }

        string trimmed = line.TrimStart();

        switch (currentSection)
        {
          case "FORMAT": break;
          case "METADATA":
            ParseChartMetadataLine(trimmed, data.Metadata);
            break;
          case "OVERLAYS":
            ParseOverlayLine(trimmed, data.Overlays, ref currentOverlay);
            break;
          case "COMPONENTS":
            ParseComponentLine(trimmed, data.Components, ref currentComponent);
            break;
          case "THEME_CHANNELS":
            ParseThemeChannelLine(trimmed, data.ThemeChannels, ref currentTheme);
            break;
          case "GROUPS":
            ParseGroupLine(trimmed, data.Groups, ref currentGroup);
            break;
          case "WINDOWS":
            ParseWindowLine(trimmed, data.Windows, ref currentWindow,
                            ref currentSpeedStep, ref currentNote);
            break;
        }
      }

      ParserUtils.CullNotesOutsideWindows(data);
      ParserUtils.ResolveInheritance(data);
    }

    // ── METADATA ──

    private static void ParseChartMetadataLine(string line, ChartMetadata meta)
    {
      if (ParserUtils.TryParseProperty(line, "Index:", out string index))
      { int.TryParse(index, out int idx); meta.Index = idx; }
      else if (ParserUtils.TryParseProperty(line, "Name:", out string name))
        meta.ChartName = name;
      else if (ParserUtils.TryParseProperty(line, "Level:", out string level))
        meta.Level = level;
      else if (ParserUtils.TryParseProperty(line, "Constant:", out string constant))
        meta.Constant = ParserUtils.ParseFloat(constant);
    }

    // ── OVERLAYS ──

    private static void ParseOverlayLine(string trimmed, List<OverlayData> overlays, ref OverlayData current)
    {
      if (trimmed.StartsWith("+ "))
      {
        current = new OverlayData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1)
          current.ID = parts[0];
        for (int j = 1; j < parts.Length; j++)
        {
          string p = parts[j];
          if (p == "-")
          {
            current.InitParams.Add(new VectorValue { IsDefault = true });
          }
          else if (VectorValue.IsVectorFormat(p))
          {
            current.InitParams.Add(VectorValue.Parse(p));
          }
          else
          {
            // size 1 vector
            current.InitParams.Add(new VectorValue(ParserUtils.ParseFloat(p)));
          }
        }
        overlays.Add(current);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Shader:", out string shader))
        current.ShaderFile = shader;
      else if (trimmed.StartsWith("/ "))
        current.Events.Add(ParserUtils.ParseStoryboardEvent(trimmed));
    }

    // ── COMPONENTS ──

    private static void ParseComponentLine(
        string trimmed, List<ComponentData> components, ref ComponentData current)
    {
      if (trimmed.StartsWith("* "))
      {
        current = new ComponentData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.Type = ComponentData.ParseType(parts[0]);
        if (parts.Length >= 2) current.InitX = ParserUtils.ParseFloat(parts[1]);
        if (parts.Length >= 3) current.InitY = ParserUtils.ParseFloat(parts[2]);
        if (parts.Length >= 4) current.InitScale = ParserUtils.ParseFloat(parts[3]);
        if (parts.Length >= 5) current.InitAlpha = ParserUtils.ParseFloat(parts[4]);
        if (parts.Length >= 6) current.AnchorX = ParserUtils.ParseFloat(parts[5]);
        if (parts.Length >= 7) current.AnchorY = ParserUtils.ParseFloat(parts[6]);
        components.Add(current);
        return;
      }

      if (current == null) return;

      if (trimmed.StartsWith("/ "))
        current.Events.Add(ParserUtils.ParseStoryboardEvent(trimmed));
    }

    // ── THEME CHANNELS ──

    private static void ParseThemeChannelLine(
        string trimmed, List<ThemeChannelData> themes, ref ThemeChannelData current)
    {
      if (trimmed.StartsWith("+ "))
      {
        current = new ThemeChannelData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.ID = parts[0];
        if (parts.Length >= 2) current.InitR = ParserUtils.ParseFloat(parts[1]);
        if (parts.Length >= 3) current.InitG = ParserUtils.ParseFloat(parts[2]);
        if (parts.Length >= 4) current.InitB = ParserUtils.ParseFloat(parts[3]);
        if (parts.Length >= 5) current.InitA = ParserUtils.ParseFloat(parts[4]);
        if (parts.Length >= 6) current.InitNoteA = ParserUtils.ParseFloat(parts[5]);
        themes.Add(current);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (trimmed.StartsWith("/ "))
        current.Events.Add(ParserUtils.ParseStoryboardEvent(trimmed));
    }

    // ── GROUPS ──

    private static void ParseGroupLine(
        string trimmed, List<GroupData> groups, ref GroupData current)
    {
      if (trimmed.StartsWith("+ "))
      {
        current = new GroupData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.ID = parts[0];
        if (parts.Length >= 2) current.InitX = ParserUtils.ParseFloat(parts[1]);
        if (parts.Length >= 3) current.InitY = ParserUtils.ParseFloat(parts[2]);
        if (parts.Length >= 4) current.InitScaleX = ParserUtils.ParseFloat(parts[3]);
        if (parts.Length >= 5) current.InitScaleY = ParserUtils.ParseFloat(parts[4]);
        if (parts.Length >= 6) current.InitRotation = ParserUtils.ParseFloat(parts[5]);
        groups.Add(current);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Group:", out string groupId))
        current.ParentGroupID = groupId;
      else if (trimmed.StartsWith("/ "))
        current.Events.Add(ParserUtils.ParseStoryboardEvent(trimmed));
    }

    // ── WINDOWS ──

    private static void ParseWindowLine(
        string trimmed, List<WindowData> windows,
        ref WindowData current, ref SpeedStep currentSpeedStep, ref NoteData currentNote)
    {
      if (trimmed.StartsWith("+ "))
      {
        current = new WindowData();
        currentSpeedStep = null;
        currentNote = null;
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.ID = parts[0];
        if (parts.Length >= 2) current.IsUnFocus = ParserUtils.ParseBool(parts[1]);
        if (parts.Length >= 3) current.InitX = ParserUtils.ParseFloat(parts[2]);
        if (parts.Length >= 4) current.InitY = ParserUtils.ParseFloat(parts[3]);
        if (parts.Length >= 5) current.InitScaleX = Math.Abs(ParserUtils.ParseFloat(parts[4]));
        if (parts.Length >= 6) current.InitScaleY = Math.Abs(ParserUtils.ParseFloat(parts[5]));
        if (parts.Length >= 7) current.InitR = ParserUtils.ParseFloat(parts[6]);
        if (parts.Length >= 8) current.InitG = ParserUtils.ParseFloat(parts[7]);
        if (parts.Length >= 9) current.InitB = ParserUtils.ParseFloat(parts[8]);
        if (parts.Length >= 10) current.InitA = ParserUtils.ParseFloat(parts[9]);
        if (parts.Length >= 11) current.InitNoteA = ParserUtils.ParseFloat(parts[10]);
        windows.Add(current);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Title:", out string title))
        current.Title = title;
      else if (ParserUtils.TryParseProperty(trimmed, "Layer:", out string layer))
      { int.TryParse(layer, out int lyr); current.Layer = lyr; }
      else if (ParserUtils.TryParseProperty(trimmed, "Anchor:", out string anchor))
      {
        string[] parts = anchor.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
          current.AnchorX = ParserUtils.ParseFloat(parts[0]);
          current.AnchorY = ParserUtils.ParseFloat(parts[1]);
        }
      }
      else if (ParserUtils.TryParseProperty(trimmed, "Group:", out string groupId))
        current.GroupID = groupId;
      else if (ParserUtils.TryParseProperty(trimmed, "Theme Channel:", out string themeId))
        current.ThemeChannelID = themeId;
      else if (trimmed.StartsWith("| "))
      {
        currentSpeedStep = new SpeedStep();
        currentNote = null;
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) currentSpeedStep.Start = BeatTime.Parse(parts[0]);
        if (parts.Length >= 2) currentSpeedStep.Multiplier = ParserUtils.ParseFloat(parts[1]);
        current.SpeedSteps.Add(currentSpeedStep);
      }
      else if (trimmed.StartsWith("# "))
      {
        currentNote = new NoteData();
        currentSpeedStep = null;
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) currentNote.Type = NoteData.ParseNoteType(parts[0]);
        if (parts.Length >= 2) currentNote.Start = BeatTime.Parse(parts[1]);
        if (parts.Length >= 3) currentNote.Length = BeatTime.Parse(parts[2]);
        if (parts.Length >= 4) currentNote.Side = NoteData.ParseSide(parts[3]);
        if (parts.Length >= 5) { int.TryParse(parts[4], out int fake); currentNote.FakeType = fake; }
        current.Notes.Add(currentNote);
      }
      else if (trimmed.StartsWith("/ "))
      {
        StoryboardEvent evt = ParserUtils.ParseStoryboardEvent(trimmed);

        // Enforce absolute scales on Windows to avoid clipping / title bar flipping
        if (evt.Type == StoryboardProperty.ScaleX || evt.Type == StoryboardProperty.ScaleY)
        {
          evt.FromValue = Math.Abs(evt.FromValue);
          evt.ToValue = Math.Abs(evt.ToValue);
        }

        if (currentNote != null)
          currentNote.Events.Add(evt);
        else if (currentSpeedStep != null)
          currentSpeedStep.Events.Add(evt);
        else
          current.Events.Add(evt);
      }
    }
  }
}
