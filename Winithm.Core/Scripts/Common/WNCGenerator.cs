using Godot;
using System;
using System.Linq;
using System.Text;
using Winithm.Core.Data;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Generator for .wnc (Chart) files.
  /// Writes ChartData back into the Winithm .wnc plaintext format.
  /// </summary>
  public static class WNCGenerator
  {
    public static readonly string CHART_FORMAT_VERSION = "1.2";

    public static void Generate(string filePath, ChartData data)
    {
      var sb = new StringBuilder();

      // [FORMAT]
      sb.AppendLine("[FORMAT]");
      sb.AppendLine("Type: Chart");
      sb.AppendLine($"Version: {CHART_FORMAT_VERSION}");
      sb.AppendLine();

      // [METADATA]
      sb.AppendLine("[METADATA]");
      sb.AppendLine($"Index: {data.Metadata.Index}");
      sb.AppendLine($"ID: {data.Metadata.ChartID}");
      sb.AppendLine($"Name: {data.Metadata.ChartName}");
      sb.AppendLine($"Level: {data.Metadata.Level}");
      sb.AppendLine($"Constant: {ParserUtils.FormatFloat(data.Metadata.Constant)}");
      sb.AppendLine();

      // [OVERLAYS]
      if (data.Overlays.Count > 0)
      {
        sb.AppendLine("[OVERLAYS]");
        foreach (var overlay in data.Overlays)
        {
          string initParams = "";
          foreach (var p in overlay.InitParams.OrderBy(kv => int.TryParse(kv.Key, out int i) ? i : int.MaxValue))
            initParams += " " + p.Value.ToString();
          sb.AppendLine($"+ {overlay.ID}{initParams}");
          sb.AppendLine($"  Name: {overlay.Name ?? ""}");
          sb.AppendLine($"  Shader: {overlay.ShaderFile}");
          sb.AppendLine($"  Affects UI: {Convert.ToInt32(overlay.AffectsUI)}");
          if (overlay.StoryboardEvents != null)
            foreach (var kvp in overlay.StoryboardEvents)
              foreach (var evt in kvp.Value)
                sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt, StoryboardProperty.Custom, kvp.Key));
        }
        sb.AppendLine();
      }

      // [COMPONENTS]
      if (data.Components.Count > 0)
      {
        sb.AppendLine("[COMPONENTS]");
        foreach (var comp in data.Components)
        {
          sb.AppendLine($"* {comp.Type} {ParserUtils.FormatFloat(comp.InitX)} {ParserUtils.FormatFloat(comp.InitY)} {ParserUtils.FormatFloat(comp.InitScale)} {ParserUtils.FormatFloat(comp.InitAlpha)} {ParserUtils.FormatFloat(comp.AnchorX)} {ParserUtils.FormatFloat(comp.AnchorY)}");
          if (comp.StoryboardEvents != null)
            foreach (var kvp in comp.StoryboardEvents)
              foreach (var evt in kvp.Value)
                sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt, kvp.Key));
        }
        sb.AppendLine();
      }

      // [THEME_CHANNELS]
      if (data.ThemeChannels.Count > 0)
      {
        sb.AppendLine("[THEME_CHANNELS]");
        foreach (var tc in data.ThemeChannels)
        {
          sb.AppendLine($"+ {tc.ID} {ParserUtils.FormatFloat(tc.InitR)} {ParserUtils.FormatFloat(tc.InitG)} {ParserUtils.FormatFloat(tc.InitB)} {ParserUtils.FormatFloat(tc.InitA)} {ParserUtils.FormatFloat(tc.InitNoteA)}");
          sb.AppendLine($"  Name: {tc.Name ?? ""}");
          if (tc.StoryboardEvents != null)
            foreach (var kvp in tc.StoryboardEvents)
              foreach (var evt in kvp.Value)
                sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt, kvp.Key));
        }
        sb.AppendLine();
      }

      // [GROUPS]
      if (data.Groups.Count > 0)
      {
        sb.AppendLine("[GROUPS]");
        foreach (var g in data.Groups)
        {
          sb.AppendLine($"+ {g.ID} {ParserUtils.FormatFloat(g.InitX)} {ParserUtils.FormatFloat(g.InitY)} {ParserUtils.FormatFloat(g.InitScaleX)} {ParserUtils.FormatFloat(g.InitScaleY)} {ParserUtils.FormatFloat(g.InitRotation)}");
          sb.AppendLine($"  Name: {g.Name ?? ""}");
          sb.AppendLine($"  Group: {g.ParentGroupID ?? ""}");
          if (g.StoryboardEvents != null)
            foreach (var kvp in g.StoryboardEvents)
              foreach (var evt in kvp.Value)
                sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt, kvp.Key));
        }
        sb.AppendLine();
      }

      // [WINDOWS]
      if (data.Windows.Count > 0)
      {
        sb.AppendLine("[WINDOWS]");
        foreach (var w in data.Windows)
        {
          sb.AppendLine($"+ {w.ID} {ParserUtils.FormatFloat(w.InitX)} {ParserUtils.FormatFloat(w.InitY)} {ParserUtils.FormatFloat(w.InitScaleX)} {ParserUtils.FormatFloat(w.InitScaleY)} {ParserUtils.FormatFloat(w.InitR)} {ParserUtils.FormatFloat(w.InitG)} {ParserUtils.FormatFloat(w.InitB)} {ParserUtils.FormatFloat(w.InitA)} {ParserUtils.FormatFloat(w.InitNoteA)}");
          sb.AppendLine($"  Name: {w.Name ?? ""}");
          sb.AppendLine($"  Title: {w.Title ?? ""}");
          sb.AppendLine($"  Flags: {Convert.ToInt32(w.Borderless)} {Convert.ToInt32(w.UnFocus)}");
          sb.AppendLine($"  Anchor: {ParserUtils.FormatFloat(w.AnchorX)} {ParserUtils.FormatFloat(w.AnchorY)}");
          sb.AppendLine($"  Layer: {w.Layer}");
          sb.AppendLine($"  Group: {w.GroupID ?? ""}");
          sb.AppendLine($"  Theme Channel: {w.ThemeChannelID ?? ""}");

          // Window-level storyboard events
          if (w.StoryboardEvents != null)
            foreach (var kvp in w.StoryboardEvents)
              foreach (var evt in kvp.Value)
                sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt, kvp.Key));

          // Notes
          foreach (var ns in w.Notes.Values)
            foreach (var n in ns)
              sb.AppendLine($"  # {n.ID} {n.Type} {n.StartBeat} {n.Length} {n.Side} {n.FakeType}");

          // SpeedSteps
          foreach (var ss in w.SpeedSteps)
          {
            sb.AppendLine($"  | {ss.ID} {ss.Start} {ParserUtils.FormatFloat(ss.Multiplier)}");
            if (ss.StoryboardEvents != null)
              foreach (var kvp in ss.StoryboardEvents)
                foreach (var evt in kvp.Value)
                  sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt, kvp.Key, indent: "    "));
          }
        }
      }

      var file = new File();
      file.Open(filePath, File.ModeFlags.Write);
      file.StoreString(sb.ToString());
      file.Close();
    }
  }
}
