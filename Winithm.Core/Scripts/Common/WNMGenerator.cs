using Godot;
using System;
using System.Text;
using Winithm.Core.Data;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Generator for .wnm (Metadata) files.
  /// Writes ChartData back into the Winithm .wnm plaintext format.
  /// </summary>
  public static class WNMGenerator
  {
    public static readonly string METADATA_FORMAT_VERSION = "1.0";

    public static void Generate(string filePath, SongMetaData data)
    {
      var sb = new StringBuilder();

      // [FORMAT]
      sb.AppendLine("[FORMAT]");
      sb.AppendLine("Type: Metadata");
      sb.AppendLine($"Version: {METADATA_FORMAT_VERSION}");
      sb.AppendLine();

      // [METADATA]
      sb.AppendLine("[METADATA]");
      sb.AppendLine($"ID: {data.ID}");
      sb.AppendLine($"Name: {data.Name}");
      sb.AppendLine($"Name Alt: {data.NameAlt}");
      sb.AppendLine($"Artist: {data.Artist}");
      sb.AppendLine($"Artist Alt: {data.ArtistAlt}");
      sb.AppendLine($"Tags: {data.Tags}");
      sb.AppendLine($"Preview Range: {ParserUtils.FormatFloat(data.PreviewStart)} {ParserUtils.FormatFloat(data.PreviewEnd)}");
      sb.AppendLine();

      // [RESOURCES]
      sb.AppendLine("[RESOURCES]");
      sb.AppendLine("* Song");
      sb.AppendLine($"  Path: {data.Resources.SongPath}");
      sb.AppendLine($"  Base BPM: {ParserUtils.FormatFloat(data.Resources.BaseBPM.BaseOffsetSeconds)} {ParserUtils.FormatFloat(data.Resources.BaseBPM.InitialBPM)} {data.Resources.BaseBPM.TimeSignature}");
      sb.AppendLine("  BPM List:");
      foreach (var bpm in data.Resources.BPMList)
        sb.AppendLine($"  + {bpm.StartBeat} {ParserUtils.FormatFloat(bpm.BPM)} {bpm.TimeSignature}");

      sb.AppendLine("* Illustration");
      sb.AppendLine($"  Illustrator: {data.Resources.Illustrator}");
      sb.AppendLine($"  Path: {data.Resources.IllustrationPath}");
      sb.AppendLine($"  Icon Center: {ParserUtils.FormatFloat(data.Resources.IconCenterX)} {ParserUtils.FormatFloat(data.Resources.IconCenterY)}");
      sb.AppendLine($"  Icon Size: {ParserUtils.FormatFloat(data.Resources.IconSize)}");
      sb.AppendLine();

      // [CHARTS]
      sb.AppendLine("[CHARTS]");
      foreach (var chart in data.Charts)
      {
        sb.AppendLine($"+ ID: {chart.ID}");
        sb.AppendLine($"  Index: {chart.Index}");
        sb.AppendLine($"  Name: {chart.Name}");
        sb.AppendLine($"  Charter: {chart.Charter}");
        sb.AppendLine($"  Level: {chart.Level}");
        sb.AppendLine($"  Constant: {ParserUtils.FormatFloat(chart.Constant)}");
      }

      var file = new File();
      file.Open(filePath, File.ModeFlags.Write);
      file.StoreString(sb.ToString());
      file.Close();
    }
  }
}
