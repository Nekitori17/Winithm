using System;
using System.IO;
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
    public static void Generate(string filePath, ChartData data)
    {
      var sb = new StringBuilder();

      // [FORMAT]
      sb.AppendLine("[FORMAT]");
      sb.AppendLine("Type: Chart");
      sb.AppendLine("Version: 1");
      sb.AppendLine();

      // [METADATA]
      sb.AppendLine("[METADATA]");
      sb.AppendLine($"Index: {data.Metadata.Index}");
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
          foreach (var p in overlay.InitParams)
            initParams += " " + p.ToString();
          sb.AppendLine($"+ {overlay.ID}{initParams}");
          sb.AppendLine($"  Name: {overlay.Name}");
          sb.AppendLine($"  Shader: {overlay.ShaderFile}");
          foreach (var evt in overlay.Events)
            sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt));
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
          foreach (var evt in comp.Events)
            sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt));
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
          sb.AppendLine($"  Name: {tc.Name}");
          foreach (var evt in tc.Events)
            sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt));
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
          sb.AppendLine($"  Name: {g.Name}");
          if (!string.IsNullOrEmpty(g.ParentGroupID))
            sb.AppendLine($"  Group: {g.ParentGroupID}");
          foreach (var evt in g.Events)
            sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt));
        }
        sb.AppendLine();
      }

      // [WINDOWS]
      if (data.Windows.Count > 0)
      {
        sb.AppendLine("[WINDOWS]");
        foreach (var w in data.Windows)
        {
          sb.AppendLine($"+ {w.ID} {Convert.ToInt32(w.IsUnFocus)} {ParserUtils.FormatFloat(w.InitX)} {ParserUtils.FormatFloat(w.InitY)} {ParserUtils.FormatFloat(w.InitScaleX)} {ParserUtils.FormatFloat(w.InitScaleY)} {ParserUtils.FormatFloat(w.InitR)} {ParserUtils.FormatFloat(w.InitG)} {ParserUtils.FormatFloat(w.InitB)} {ParserUtils.FormatFloat(w.InitA)} {ParserUtils.FormatFloat(w.InitNoteA)}");
          sb.AppendLine($"  Name: {w.Name}");
          if (!string.IsNullOrEmpty(w.Title))
            sb.AppendLine($"  Title: {w.Title}");
          sb.AppendLine($"  Anchor: {ParserUtils.FormatFloat(w.AnchorX)} {ParserUtils.FormatFloat(w.AnchorY)}");
          sb.AppendLine($"  Layer: {w.Layer}");
          if (!string.IsNullOrEmpty(w.GroupID))
            sb.AppendLine($"  Group: {w.GroupID}");
          if (!string.IsNullOrEmpty(w.ThemeChannelID))
            sb.AppendLine($"  Theme Channel: {w.ThemeChannelID}");

          // Window-level storyboard events
          foreach (var evt in w.Events)
            sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt));

          // Notes (skip notes that start before window StartBeat or after window EndBeat)
          float startBeat = w.StartBeat;
          float endBeat = w.EndBeat;
          foreach (var n in w.Notes)
          {
            if (n.Start.AbsoluteValue < startBeat || n.Start.AbsoluteValue > endBeat) continue;
            sb.AppendLine($"  # {n.Type} {n.Start} {n.Length} {n.Side} {n.FakeType}");
            foreach (var evt in n.Events)
              sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt, "    "));
          }

          // SpeedSteps
          foreach (var ss in w.SpeedSteps)
          {
            sb.AppendLine($"  | {ss.Start} {ParserUtils.FormatFloat(ss.Multiplier)}");
            foreach (var evt in ss.Events)
              sb.AppendLine(ParserUtils.GenerateStoryboardEventLine(evt, "    "));
          }
        }
      }

      File.WriteAllText(filePath, sb.ToString());
    }
  }
}
