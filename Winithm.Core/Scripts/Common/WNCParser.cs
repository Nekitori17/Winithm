using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Managers;

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
      SpeedStepData currentSpeedStep = null;

      data.Components.BeginUpdate();
      data.ThemeChannels.BeginUpdate();
      data.Groups.BeginUpdate();
      data.Windows.BeginUpdate();
      data.Overlays.BeginUpdate();

      try
      {
        string line;
        while (!file.EofReached())
        {
          line = file.GetLine().TrimEnd();
          if (string.IsNullOrWhiteSpace(line)) continue;

          if (line.StartsWith("[") && line.EndsWith("]"))
          {
            currentComponent?.StoryboardEvents?.EndUpdate();
            currentTheme?.StoryboardEvents?.EndUpdate();
            currentGroup?.StoryboardEvents?.EndUpdate();
            currentWindow?.StoryboardEvents?.EndUpdate();
            currentWindow?.Notes?.EndUpdate();
            currentWindow?.SpeedSteps?.EndUpdate();
            currentSpeedStep?.StoryboardEvents?.EndUpdate();
            currentOverlay?.StoryboardEvents?.EndUpdate();

            currentSection = line.Substring(1, line.Length - 2);
            currentComponent = null;
            currentTheme = null;
            currentGroup = null;
            currentWindow = null;
            currentOverlay = null;
            continue;
          }

          string trimmed = line.TrimStart();

          switch (currentSection)
          {
            case "FORMAT": break;
            case "METADATA":
              ParseChartMetadataLine(trimmed, data.ChartMetadata);
              break;
            case "OVERLAYS":
              ParseOverlayLine(trimmed, data.Overlays, currentOverlay, data.ObjectFactory);
              break;
            case "COMPONENTS":
              ParseComponentLine(trimmed, data.Components, currentComponent, data.ObjectFactory);
              break;
            case "THEME_CHANNELS":
              ParseThemeChannelLine(trimmed, data.ThemeChannels, currentTheme, data.ObjectFactory);
              break;
            case "GROUPS":
              ParseGroupLine(trimmed, data.Groups, currentGroup, data.ObjectFactory);
              break;
            case "WINDOWS":
              ParseWindowLine(trimmed, data.Windows, currentWindow, currentSpeedStep, data.ObjectFactory);
              break;
            default:
              GD.PushWarning($"Unknown section: {currentSection}");
              break;
          }
        }

        // End updates for any lingering objects
        currentComponent?.StoryboardEvents?.EndUpdate();
        currentTheme?.StoryboardEvents?.EndUpdate();
        currentGroup?.StoryboardEvents?.EndUpdate();
        currentWindow?.StoryboardEvents?.EndUpdate();
        currentWindow?.Notes?.EndUpdate();
        currentWindow?.SpeedSteps?.EndUpdate();
        currentSpeedStep?.StoryboardEvents?.EndUpdate();
        currentOverlay?.StoryboardEvents?.EndUpdate();

        data.Components.EndUpdate();
        data.ThemeChannels.EndUpdate();
        data.Groups.EndUpdate();
        data.Windows.EndUpdate();
        data.Overlays.EndUpdate();
      }
      finally
      {
        file.Close();
      }

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

    private static void ParseOverlayLine(
      string trimmed, OverlayManager overlays, OverlayData current, ObjectFactory factory
    )
    {
      if (trimmed.StartsWith("+ "))
      {
        current?.StoryboardEvents.EndUpdate();

        current = new OverlayData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 1) current.ID = parts[0];
        if (parts.Length >= 2) current.StartBeat =
          BeatTime.TryParse(parts[1], out var sb) ? sb : BeatTime.Zero;
        if (parts.Length >= 3) current.EndBeat =
          BeatTime.TryParse(parts[2], out var eb) ? eb : BeatTime.Zero;

        for (int j = 3; j < parts.Length; j++)
        {
          string p = parts[j];
          string key = (j - 1).ToString();
          AnyValue val = p == "-"
            ? new AnyValue { Type = AnyValueType.Inherited }
            : AnyValue.Parse(p);

          current.InitParams[key] = val;
          current.ShaderParams[key] = new ShaderParamDef(val.Type, val);
        }
        overlays.AddOverlay(current);

        factory.SyncMaxIDSeed(current.ID);
        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Shader:", out string shader))
        current.ShaderFile = shader;
      else if (ParserUtils.TryParseProperty(trimmed, "Affects UI:", out string affectsUI))
        current.AffectsUI = ParserUtils.ParseIntBool(affectsUI);
      else if (ParserUtils.TryParseProperty(trimmed, "Layer:", out string layer))
      { int.TryParse(layer, out int lyr); current.Layer = lyr; }
      else if (trimmed.StartsWith("/ "))
      {
        var evt = StoryboardManager<string>.ParseEventLine(trimmed, out var type, out string rawType);
        factory.SyncMaxIDSeed(evt.ID);
        if (type == StoryboardProperty.Custom)
          current.StoryboardEvents.AddEvent(rawType, evt);
      }
    }

    // ── COMPONENTS ──

    private static void ParseComponentLine(
        string trimmed, ComponentManager components, ComponentData current, ObjectFactory factory)
    {
      if (trimmed.StartsWith("* "))
      {
        current?.StoryboardEvents.EndUpdate();

        current = new ComponentData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        ComponentType type = ComponentType.Info;

        if (parts.Length >= 1) type =
          Enum.TryParse(parts[0], true, out ComponentType t) ? t : ComponentType.Info;
        if (parts.Length >= 2) current.InitX =
          ParserUtils.TryParseFloat(parts[1], out float x) ? x : 0f;
        if (parts.Length >= 3) current.InitY =
          ParserUtils.TryParseFloat(parts[2], out float y) ? y : 0f;
        if (parts.Length >= 4) current.InitScale =
          ParserUtils.TryParseFloat(parts[3], out float scale) ? scale : 1f;
        if (parts.Length >= 5) current.InitAlpha =
          ParserUtils.TryParseFloat(parts[4], out float alpha) ? alpha : 1f;


        components.SetComponent(type, current);
        return;
      }

      if (current == null) return;

      if (trimmed.StartsWith("/ "))
      {
        var evt = StoryboardManager<StoryboardProperty>.ParseEventLine(trimmed, out var type, out _);
        factory.SyncMaxIDSeed(evt.ID);
        if (type != StoryboardProperty.Custom)
          current.StoryboardEvents.AddEvent(type, evt);
      }
    }

    // ── THEME CHANNELS ──

    private static void ParseThemeChannelLine(
        string trimmed, ThemeChannelManager themes, ThemeChannelData current, ObjectFactory factory)
    {
      if (trimmed.StartsWith("+ "))
      {
        current?.StoryboardEvents.EndUpdate();

        current = new ThemeChannelData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.ID = parts[0];
        if (parts.Length >= 2) current.InitR =
          ParserUtils.TryParseFloat(parts[1], out float r) ? r : 0f;
        if (parts.Length >= 3) current.InitG =
          ParserUtils.TryParseFloat(parts[2], out float g) ? g : 0f;
        if (parts.Length >= 4) current.InitB =
          ParserUtils.TryParseFloat(parts[3], out float b) ? b : 0f;
        if (parts.Length >= 5) current.InitA =
          ParserUtils.TryParseFloat(parts[4], out float a) ? a : 1f;
        if (parts.Length >= 6) current.InitNoteA =
          ParserUtils.TryParseFloat(parts[5], out float noteA) ? noteA : 1f;

        factory.SyncMaxIDSeed(current.ID);
        themes.AddThemeChannel(current);

        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (trimmed.StartsWith("/ "))
      {
        var evt = StoryboardManager<StoryboardProperty>.ParseEventLine(trimmed, out var type, out _);
        factory.SyncMaxIDSeed(evt.ID);
        if (type != StoryboardProperty.Custom)
          current.StoryboardEvents.AddEvent(type, evt);
      }
    }

    // ── GROUPS ──

    private static void ParseGroupLine(string trimmed, GroupManager groups, GroupData current, ObjectFactory factory)
    {
      if (trimmed.StartsWith("+ "))
      {
        current?.StoryboardEvents.EndUpdate();

        current = new GroupData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.ID = parts[0];
        if (parts.Length >= 2) current.InitX =
          ParserUtils.TryParseFloat(parts[1], out float x) ? x : 0f;
        if (parts.Length >= 3) current.InitY =
          ParserUtils.TryParseFloat(parts[2], out float y) ? y : 0f;
        if (parts.Length >= 4) current.InitScaleX =
          ParserUtils.TryParseFloat(parts[3], out float sx) ? sx : 1f;
        if (parts.Length >= 5) current.InitScaleY =
          ParserUtils.TryParseFloat(parts[4], out float sy) ? sy : 1f;
        if (parts.Length >= 6) current.InitRotation =
          ParserUtils.TryParseFloat(parts[5], out float r) ? r : 0f;

        factory.SyncMaxIDSeed(current.ID);
        groups.AddGroup(current);

        return;
      }

      if (current == null) return;

      if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Group:", out string groupId))
        current.ParentGroupID = groupId;
      else if (trimmed.StartsWith("/ "))
      {
        var evt = StoryboardManager<StoryboardProperty>.ParseEventLine(trimmed, out var type, out _);
        factory.SyncMaxIDSeed(evt.ID);
        if (type != StoryboardProperty.Custom)
          current.StoryboardEvents.AddEvent(type, evt);
      }
    }

    // ── WINDOWS ──

    private static void ParseWindowLine(
        string trimmed,
        WindowManager windows,
        WindowData current,
        SpeedStepData currentSpeedStep,
        ObjectFactory factory
    )
    {
      if (trimmed.StartsWith("+ "))
      {
        current?.StoryboardEvents.EndUpdate();
        current?.SpeedSteps?.EndUpdate();
        currentSpeedStep?.StoryboardEvents.EndUpdate();
        current?.Notes?.EndUpdate();

        current = new WindowData();
        currentSpeedStep = null;
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) current.ID = parts[0];
        if (parts.Length >= 2) current.InitX =
          ParserUtils.TryParseFloat(parts[1], out float x) ? x : 0f;
        if (parts.Length >= 3) current.InitY =
          ParserUtils.TryParseFloat(parts[2], out float y) ? y : 0f;
        if (parts.Length >= 4) current.InitScaleX =
          ParserUtils.TryParseFloat(parts[3], out float sx) ? sx : 1f;
        if (parts.Length >= 5) current.InitScaleY =
          ParserUtils.TryParseFloat(parts[4], out float sy) ? sy : 1f;
        if (parts.Length >= 6) current.InitR =
          ParserUtils.TryParseFloat(parts[5], out float r) ? r : 0f;
        if (parts.Length >= 7) current.InitG =
          ParserUtils.TryParseFloat(parts[6], out float g) ? g : 0f;
        if (parts.Length >= 8) current.InitB =
          ParserUtils.TryParseFloat(parts[7], out float b) ? b : 0f;
        if (parts.Length >= 9) current.InitA =
          ParserUtils.TryParseFloat(parts[8], out float a) ? a : 1f;
        if (parts.Length >= 10) current.InitNoteA =
          ParserUtils.TryParseFloat(parts[9], out float na) ? na : 1f;

        factory.SyncMaxIDSeed(current.ID);
        windows.AddWindow(current);
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
        if (parts.Length >= 1) current.Borderless =
          ParserUtils.TryParseIntBool(parts[0], out bool bl) ? bl : false;
        if (parts.Length >= 2) current.UnFocus =
          ParserUtils.TryParseIntBool(parts[1], out bool uf) ? uf : false;
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
        currentSpeedStep?.StoryboardEvents?.EndUpdate();

        currentSpeedStep = new SpeedStepData();
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) currentSpeedStep.ID = parts[0];
        if (parts.Length >= 2) currentSpeedStep.StartBeat =
          BeatTime.TryParse(parts[1], out var sb) ? sb : BeatTime.Zero;
        if (parts.Length >= 3) currentSpeedStep.Multiplier =
          ParserUtils.TryParseFloat(parts[2], out var m) ? m : 1.0f;

        factory.SyncMaxIDSeed(currentSpeedStep.ID);
        current.SpeedSteps.AddSpeedStep(currentSpeedStep);
      }
      else if (trimmed.StartsWith("# "))
      {
        var currentNote = NoteManager.ParseNoteLine(trimmed, out var side);
        factory.SyncMaxIDSeed(currentNote.ID);
        current.Notes.AddNote(side, currentNote);
      }
      else if (trimmed.StartsWith("/ "))
      {
        var evt = StoryboardManager<StoryboardProperty>.ParseEventLine(trimmed, out var type, out _);
        factory.SyncMaxIDSeed(evt.ID);

        if (currentSpeedStep != null)
          currentSpeedStep.StoryboardEvents.AddEvent(type, evt);
        else if (type != StoryboardProperty.Custom)
          current.StoryboardEvents.AddEvent(type, evt);
      }
    }
  }
}
