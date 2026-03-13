using System;
using System.IO;
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
        public static void Generate(string filePath, ChartData data)
        {
            var sb = new StringBuilder();

            // [FORMAT]
            sb.AppendLine("[FORMAT]");
            sb.AppendLine("Type: Metadata");
            sb.AppendLine("Version: 1");
            sb.AppendLine();

            // [METADATA]
            sb.AppendLine("[METADATA]");
            sb.AppendLine($"ID: {data.Metadata.ID}");
            sb.AppendLine($"Name: {data.Metadata.Name}");
            sb.AppendLine($"Name Alt: {data.Metadata.NameAlt}");
            sb.AppendLine($"Artist: {data.Metadata.Artist}");
            sb.AppendLine($"Artist Alt: {data.Metadata.ArtistAlt}");
            sb.AppendLine($"Tags: {data.Metadata.Tags}");
            sb.AppendLine($"Preview Range: {ParserUtils.FormatFloat(data.Metadata.PreviewStart)} {ParserUtils.FormatFloat(data.Metadata.PreviewEnd)}");
            sb.AppendLine();

            // [RESOURCES]
            sb.AppendLine("[RESOURCES]");
            sb.AppendLine("* Song");
            sb.AppendLine($"  Path: {data.Resources.SongPath}");
            sb.AppendLine("  BPM List:");
            foreach (var bpm in data.Resources.BPMList)
                sb.AppendLine($"  + {ParserUtils.FormatFloat(bpm.StartTimeSeconds)} {ParserUtils.FormatFloat(bpm.BPM)} {bpm.TimeSignature}");

            sb.AppendLine("* Illustration");
            sb.AppendLine($"  Illustrator: {data.Resources.Illustrator}");
            sb.AppendLine($"  Path: {data.Resources.IllustrationPath}");
            sb.AppendLine($"  Icon Center: {ParserUtils.FormatFloat(data.Resources.IconCenterX)} {ParserUtils.FormatFloat(data.Resources.IconCenterY)}");
            sb.AppendLine($"  Icon Size: {ParserUtils.FormatFloat(data.Resources.IconSize)}");
            sb.AppendLine();

            // [CHARTS]
            sb.AppendLine("[CHARTS]");
            foreach (var chart in data.ChartReferences)
            {
                sb.AppendLine($"+ ID: {chart.ID}");
                sb.AppendLine($"  Index: {chart.Index}");
                sb.AppendLine($"  Name: {chart.Name}");
                sb.AppendLine($"  Charter: {chart.Charter}");
                sb.AppendLine($"  Level: {chart.Level}");
                sb.AppendLine($"  Constant: {ParserUtils.FormatFloat(chart.Constant)}");
            }

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
