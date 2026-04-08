using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Parser for .wnm (Metadata) files.
  /// Reads [FORMAT], [METADATA], [RESOURCES], and [CHARTS] sections.
  /// </summary>
  public static class WNMParser
  {
    /// <summary>Parse a .wnm metadata file.</summary>
    public static SongMetaData Parse(string filePath)
    {
      var data = new SongMetaData();
      var file = new File();
      file.Open(filePath, File.ModeFlags.Read);

      if (file.GetError() != Error.Ok)
      {
        System.Diagnostics.Trace.TraceError($"[WNMParser] Failed to open file: {filePath}");
        return data;
      }

      string currentSection = "";
      string currentResource = "";
      ChartReference currentChart = null;

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
            continue;
          }

          switch (currentSection)
          {
            case "FORMAT": break;
            case "METADATA":
              ParseMetadataLine(line, data);
              break;
            case "RESOURCES":
              ParseResourceLine(line, data.Resources, ref currentResource);
              break;
            case "CHARTS":
              currentChart = ParseChartReferenceLine(line, data.Charts, currentChart);
              break;
          }
        }
      }
      finally
      {
        file.Close();
      }
      return data;
    }

    private static void ParseMetadataLine(string line, SongMetaData meta)
    {
      string trimmed = line.TrimStart();
      if (ParserUtils.TryParseProperty(trimmed, "ID:", out string id)) meta.ID = id;
      else if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name)) meta.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Name Alt:", out string nameAlt)) meta.NameAlt = nameAlt;
      else if (ParserUtils.TryParseProperty(trimmed, "Artist:", out string artist)) meta.Artist = artist;
      else if (ParserUtils.TryParseProperty(trimmed, "Artist Alt:", out string artistAlt)) meta.ArtistAlt = artistAlt;
      else if (ParserUtils.TryParseProperty(trimmed, "Tags:", out string tags)) meta.Tags = tags;
      else if (ParserUtils.TryParseProperty(trimmed, "Preview Range:", out string range))
      {
        string[] parts = range.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
          meta.PreviewStart = ParserUtils.ParseFloat(parts[0]);
          meta.PreviewEnd = ParserUtils.ParseFloat(parts[1]);
        }
      }
    }

    private static void ParseResourceLine(string line, ResourceData res, ref string currentResource)
    {
      string trimmed = line.TrimStart();

      if (trimmed.StartsWith("* "))
      {
        currentResource = trimmed.Substring(2).Trim();
        return;
      }

      if (trimmed.StartsWith("+ ") && currentResource == "Song")
      {
        string[] parts = trimmed.Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
          int.TryParse(parts[2], out int timeSig);
          var stop = new BPMStop(
              BeatTime.Parse(parts[0]),
              ParserUtils.ParseFloat(parts[1]),
              timeSig
          );
          res.BPMList.Add(stop);
        }
        return;
      }

      switch (currentResource)
      {
        case "Song":
          if (ParserUtils.TryParseProperty(trimmed, "Path:", out string songPath))
            res.SongPath = songPath;
          if (ParserUtils.TryParseProperty(trimmed, "Base BPM:", out string bpmBase))
          {
            string[] parts = bpmBase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
              int.TryParse(parts[2], out int timeSig);
              var baseBPM = new BaseBPM(
                  float.TryParse(parts[0], out float startTimeSeconds) ? startTimeSeconds : 0f,
                  ParserUtils.ParseFloat(parts[1]),
                  timeSig
              );
              res.BaseBPM = baseBPM;
            }
            return;
          }
          break;
        case "Illustration":
          if (ParserUtils.TryParseProperty(trimmed, "Illustrator:", out string illustrator))
            res.Illustrator = illustrator;
          else if (ParserUtils.TryParseProperty(trimmed, "Path:", out string illPath))
            res.IllustrationPath = illPath;
          else if (ParserUtils.TryParseProperty(trimmed, "Icon Center:", out string center))
          {
            string[] parts = center.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
              res.IconCenterX = ParserUtils.ParseFloat(parts[0]);
              res.IconCenterY = ParserUtils.ParseFloat(parts[1]);
            }
          }
          else if (ParserUtils.TryParseProperty(trimmed, "Icon Size:", out string size))
            res.IconSize = ParserUtils.ParseFloat(size);
          break;
      }
    }

    private static ChartReference ParseChartReferenceLine(
        string line, List<ChartReference> charts, ChartReference current)
    {
      string trimmed = line.TrimStart();

      if (trimmed.StartsWith("+ "))
      {
        current = new ChartReference();
        charts.Add(current);
        if (ParserUtils.TryParseProperty(trimmed.Substring(2), "ID:", out string id))
          current.ID = id;
        return current;
      }

      if (current == null) return current;

      if (ParserUtils.TryParseProperty(trimmed, "Index:", out string index))
      { int.TryParse(index, out int idx); current.Index = idx; }
      else if (ParserUtils.TryParseProperty(trimmed, "Name:", out string name))
        current.Name = name;
      else if (ParserUtils.TryParseProperty(trimmed, "Charter:", out string charter))
        current.Charter = charter;
      else if (ParserUtils.TryParseProperty(trimmed, "Level:", out string level))
        current.Level = level;
      else if (ParserUtils.TryParseProperty(trimmed, "Constant:", out string constant))
        current.Constant = ParserUtils.ParseFloat(constant);

      return current;
    }
  }
}
