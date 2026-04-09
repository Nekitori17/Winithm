using Godot;
using System;
using System.Collections.Generic;
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
      File file = new File();
      file.Open(filePath, File.ModeFlags.Read);

      if (file.GetError() != Error.Ok)
      {
        System.Diagnostics.Trace.TraceError($"[WNCParser] Failed to open file: {filePath}");
        return;
      }

      string currentSection = "";

      ComponentData currentComponent = null;
      ThemeChannelData currentTheme = null;
      GroupData currentGroup = null;
      WindowData currentWindow = null;
      OverlayData currentOverlay = null;
      SpeedStep currentSpeedStep = null;
      NoteData currentNote = null;

      try
      {
        string line;
        while (!file.EofReached())
        {
          line = file.GetLine().TrimEnd();
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
              ParseOverlayLine(trimmed, data.Overlays, ref currentOverlay, ref data.Metadata);
              break;
            case "COMPONENTS":
              ParseComponentLine(trimmed, data.Components, ref currentComponent, ref data.Metadata);
              break;
            case "THEME_CHANNELS":
              ParseThemeChannelLine(trimmed, data.ThemeChannels, ref currentTheme, ref data.Metadata);
              break;
            case "GROUPS":
              ParseGroupLine(trimmed, data.Groups, ref currentGroup, ref data.Metadata);
              break;
            case "WINDOWS":
              ParseWindowLine(trimmed, data.Windows, ref currentWindow,
                              ref currentSpeedStep, ref currentNote, ref data.Metadata);
              break;
            default:
              GD.PushWarning($"Unknown section: {currentSection}");
              break;
          }
        }
      }
      finally
      {
        file.Close();
      }

      data.Metadata.NextIDSeed++;
      PostProcess(data);
      ParserUtils.ResolveInheritance(data);
    }

    /// <summary>
    /// Sorts and precomputes all parsed data. Called once after parsing completes.
    /// </summary>
    private static void PostProcess(ChartData data)
    {
      // Overlays
      foreach (OverlayData overlay in data.Overlays)
      {
        SortStoryboardEvents(overlay.StoryboardEvents);
      }
      data.Overlays.Sort((a, b) => a.ID.CompareTo(b.ID));

      // Components
      foreach (ComponentData component in data.Components)
      {
        SortStoryboardEvents(component.StoryboardEvents);
      }
      data.Components.Sort((a, b) => a.Type.CompareTo(b.Type));

      // Theme Channels
      foreach (ThemeChannelData theme in data.ThemeChannels)
      {
        SortStoryboardEvents(theme.StoryboardEvents);
      }
      data.ThemeChannels.Sort((a, b) => a.ID.CompareTo(b.ID));

      // Groups
      foreach (GroupData group in data.Groups)
      {
        SortStoryboardEvents(group.StoryboardEvents);
      }
      data.Groups.Sort((a, b) => a.ID.CompareTo(b.ID));

      // Windows (Sort events, notes, then precompute lifecycle + MaxEndBeats)
      foreach (WindowData window in data.Windows)
      { 
        if (window.SpeedSteps.Count <= 0) continue;
        window.SpeedSteps.Sort((a, b) => a.StartBeat.AbsoluteValue.CompareTo(b.StartBeat.AbsoluteValue));
        
        foreach (SpeedStep speedstep in window.SpeedSteps)
        {
          SortStoryboardEvents(speedstep.StoryboardEvents);
        }

        SortStoryboardEvents(window.StoryboardEvents);

        if (window.Notes.Keys.Count >= 1)
          foreach (List<NoteData> notes in window.Notes.Values) {
            if (notes.Count <= 0) continue;

            notes.Sort((a, b) => a.StartBeat.AbsoluteValue.CompareTo(b.StartBeat.AbsoluteValue));
          }

        window.PreCompute();
      }
      data.Windows.Sort((a, b) => a.ID.CompareTo(b.ID));

    }

    private static void SortStoryboardEvents<TKey>(Dictionary<TKey, List<StoryboardEvent>> storyboardEvents)
    {
      if (storyboardEvents == null || storyboardEvents.Count == 0) return;

      foreach (List<StoryboardEvent> events in storyboardEvents.Values)
      {
        if (events == null || events.Count <= 0) continue;

        events.Sort((a, b) => a.StartBeat.AbsoluteValue.CompareTo(b.StartBeat.AbsoluteValue));
      }
    }

    public static void SyncMaxIDSeed(ref ChartMetadata meta, string ID)
    {
      if (string.IsNullOrEmpty(ID) || ID.Length != 6) return;

      long seed = UniqueIDGenerator.Decode(ID);
      if (seed <= 0) return;

      meta.NextIDSeed = Math.Max(meta.NextIDSeed, seed);
    }

    // ── METADATA ──

    private static void ParseChartMetadataLine(string line, ChartMetadata meta)
    {
      if (ParserUtils.TryParseProperty(line, "Index:", out string index))
        meta.Index = int.TryParse(index, out int idx) ? idx : 0;
      else if (ParserUtils.TryParseProperty(line, "ID:", out string id))
        meta.ChartID = id;
      else if (ParserUtils.TryParseProperty(line, "Name:", out string name))
        meta.ChartName = name;
      else if (ParserUtils.TryParseProperty(line, "Level:", out string level))
        meta.Level = level;
      else if (ParserUtils.TryParseProperty(line, "Constant:", out string constant))
        meta.Constant = ParserUtils.TryParseFloat(constant, out float constantValue) ? constantValue : 0f;
    }

    // ── OVERLAYS ──

    private static void ParseOverlayLine(string trimmed, List<OverlayData> overlays, ref OverlayData current, ref ChartMetadata meta)
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
          string key = (j - 1).ToString();
          AnyValue val = new AnyValue();
          AnyValueType hintType = AnyValueType.Float;
          bool hasHint = false;

          if (p.StartsWith("["))
          {
            int endIdx = p.IndexOf(']');
            if (endIdx > 0)
            {
              string hintStr = p.Substring(1, endIdx - 1);
              if (Enum.TryParse<AnyValueType>(hintStr, true, out hintType))
              {
                hasHint = true;
                p = p.Substring(endIdx + 1);
              }
            }
          }

          if (p == "-")
          {
            val = new AnyValue { Type = AnyValueType.Inherited };
          }
          else
          {
            val = AnyValue.Parse(p);
          }

          if (hasHint) val.Type = hintType;
          current.InitParams[key] = val;

          // Store metadata hint
          current.ShaderParams[key] = new ShaderParamDef(val.Type, val);
        }
        overlays.Add(current);

        SyncMaxIDSeed(ref meta, current.ID);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Shader:", out string shader))
        current.ShaderFile = shader;
      else if (ParserUtils.TryParseProperty(trimmed, "Affects UI:", out string affectsUI))
        current.AffectsUI = ParserUtils.ParseIntBool(affectsUI);
      else if (trimmed.StartsWith("/ "))
      {
        var evt = ParserUtils.ParseStoryboardEvent(trimmed, out _, out var rawName);
        SyncMaxIDSeed(ref meta, evt.ID);
        ParserUtils.AddEventToTarget(current, rawName, evt);
      }
    }

    // ── COMPONENTS ──

    private static void ParseComponentLine(
        string trimmed, List<ComponentData> components, ref ComponentData current, ref ChartMetadata meta)
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
        if (parts.Length >= 7) current.Anchor = new Vector2(
          ParserUtils.ParseFloat(parts[5]),
          ParserUtils.ParseFloat(parts[6])
        );
        components.Add(current);
        return;
      }

      if (current == null) return;

      if (trimmed.StartsWith("/ "))
      {
        var evt = ParserUtils.ParseStoryboardEvent(trimmed, out var type, out _);
        if (type != StoryboardProperty.Custom)
          ParserUtils.AddEventToTarget(current, type, evt);
      }
    }

    // ── THEME CHANNELS ──

    private static void ParseThemeChannelLine(
        string trimmed, List<ThemeChannelData> themes, ref ThemeChannelData current, ref ChartMetadata meta)
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

        SyncMaxIDSeed(ref meta, current.ID);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (trimmed.StartsWith("/ "))
      {
        var evt = ParserUtils.ParseStoryboardEvent(trimmed, out var type, out _);
        SyncMaxIDSeed(ref meta, evt.ID);
        if (type != StoryboardProperty.Custom)
          ParserUtils.AddEventToTarget(current, type, evt);
      }
    }

    // ── GROUPS ──

    private static void ParseGroupLine(
        string trimmed, List<GroupData> groups, ref GroupData current, ref ChartMetadata meta)
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

        SyncMaxIDSeed(ref meta, current.ID);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Group:", out string groupId))
        current.ParentGroupID = groupId;
      else if (trimmed.StartsWith("/ "))
      {
        var evt = ParserUtils.ParseStoryboardEvent(trimmed, out var type, out _);
        SyncMaxIDSeed(ref meta, evt.ID);
        if (type != StoryboardProperty.Custom)
          ParserUtils.AddEventToTarget(current, type, evt);
      }
    }

    // ── WINDOWS ──

    private static void ParseWindowLine(
        string trimmed, List<WindowData> windows,
        ref WindowData current, ref SpeedStep currentSpeedStep, ref NoteData currentNote, ref ChartMetadata meta)
    {
      if (trimmed.StartsWith("+ "))
      {
        current = new WindowData();
        currentSpeedStep = null;
        currentNote = null;
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.ID = parts[0];
        if (parts.Length >= 2) current.InitX = ParserUtils.ParseFloat(parts[1]);
        if (parts.Length >= 3) current.InitY = ParserUtils.ParseFloat(parts[2]);
        if (parts.Length >= 4) current.InitScaleX = Math.Abs(ParserUtils.ParseFloat(parts[3]));
        if (parts.Length >= 5) current.InitScaleY = Math.Abs(ParserUtils.ParseFloat(parts[4]));
        if (parts.Length >= 6) current.InitR = ParserUtils.ParseFloat(parts[5]);
        if (parts.Length >= 7) current.InitG = ParserUtils.ParseFloat(parts[6]);
        if (parts.Length >= 8) current.InitB = ParserUtils.ParseFloat(parts[7]);
        if (parts.Length >= 9) current.InitA = ParserUtils.ParseFloat(parts[8]);
        if (parts.Length >= 10) current.InitNoteA = ParserUtils.ParseFloat(parts[9]);
        windows.Add(current);

        SyncMaxIDSeed(ref meta, current.ID);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Title:", out string title))
        current.Title = title;
      else if (ParserUtils.TryParseProperty(trimmed, "Flags:", out string flags))
      {
        string[] parts = flags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.Borderless = ParserUtils.ParseIntBool(parts[0]);
        if (parts.Length >= 2) current.UnFocus = ParserUtils.ParseIntBool(parts[1]);
      }
      else if (ParserUtils.TryParseProperty(trimmed, "Layer:", out string layer))
      { int.TryParse(layer, out int lyr); current.Layer = lyr; }
      else if (ParserUtils.TryParseProperty(trimmed, "Anchor:", out string anchor))
      {
        string[] parts = anchor.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
          current.Anchor = new Vector2(
            ParserUtils.ParseFloat(parts[0]),
            ParserUtils.ParseFloat(parts[1])
          );
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
        if (parts.Length >= 1) currentSpeedStep.ID = parts[0];
        if (parts.Length >= 2) currentSpeedStep.StartBeat = BeatTime.Parse(parts[1]);
        if (parts.Length >= 3) currentSpeedStep.Multiplier = ParserUtils.ParseFloat(parts[2]);
        current.SpeedSteps.Add(currentSpeedStep);

        SyncMaxIDSeed(ref meta, currentSpeedStep.ID);
      }
      else if (trimmed.StartsWith("# "))
      {
        currentNote = new NoteData();
        currentSpeedStep = null;
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) currentNote.ID = parts[0];
        if (parts.Length >= 2) currentNote.Type = NoteData.ParseNoteType(parts[1]);
        if (parts.Length >= 3) currentNote.StartBeat = BeatTime.Parse(parts[2]);
        if (parts.Length >= 4) currentNote.Length =
          ParserUtils.TryParseFloat(parts[3], out float length) ? length : 0f;
        if (parts.Length >= 5) currentNote.X = ParserUtils.ParseFloat(parts[4]);
        if (parts.Length >= 6) currentNote.Width = ParserUtils.ParseFloat(parts[5]);
        if (parts.Length >= 7) currentNote.Side = NoteData.ParseSide(parts[6]);
        if (parts.Length >= 8) { int.TryParse(parts[7], out int fake); currentNote.FakeType = fake; }
        if (!current.Notes.ContainsKey(currentNote.Side))
          current.Notes[currentNote.Side] = new List<NoteData>();
        current.Notes[currentNote.Side].Add(currentNote);

        SyncMaxIDSeed(ref meta, currentNote.ID);
      }
      else if (trimmed.StartsWith("/ "))
      {
        StoryboardEvent evt = ParserUtils.ParseStoryboardEvent(trimmed, out var type, out _);
        SyncMaxIDSeed(ref meta, evt.ID);

        // Enforce absolute scales on Windows to avoid clipping / title bar flipping
        if (type == StoryboardProperty.ScaleX || type == StoryboardProperty.ScaleY)
        {
          evt.From.X = Math.Abs(evt.From.X);
          evt.To.X = Math.Abs(evt.To.X);
        }

        if (currentSpeedStep != null)
          ParserUtils.AddEventToTarget(currentSpeedStep, type, evt);
        else if (type != StoryboardProperty.Custom)
          ParserUtils.AddEventToTarget(current, type, evt);
        // Note-level events are no longer supported (NoteData has no StoryboardEvents)
      }
    }
  }
}
