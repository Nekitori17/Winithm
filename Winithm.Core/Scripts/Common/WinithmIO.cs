using Godot;
using Winithm.Core.Data;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Facade for level I/O operations. 
  /// Handles directory traversal and file loading using Godot's virtual filesystem (res://, user://).
  /// </summary>

  public static class WinithmIO
  {
    public static readonly string CHART_METADATA_FILE = "metadata.wnm";
    public static readonly string CHART_DATA_FILE = ".wnc";

    /// <summary>Loads metadata (.wnm) and a specific chart (.wnc) from a level folder.</summary>
    public static ChartData LoadLevel(string levelDir, string songID, string chartID)
    {
      var file = new File();
      string metaDataFilePath = levelDir.PlusFile(songID).PlusFile(CHART_METADATA_FILE);

      if (!file.FileExists(metaDataFilePath))
      {
        GD.PushError($"[WinithmIO] Metadata file missing: {metaDataFilePath}");
        return new ChartData();
      }


      // Load shared chart metadata
      SongMetaData songMetaData = WNMParser.Parse(metaDataFilePath);

      // Construct platform-safe path for the specific chart
      string chartFilePath = levelDir.PlusFile(songID).PlusFile(chartID + CHART_DATA_FILE);

      if (!file.FileExists(chartFilePath))
      {
        GD.PushError($"[WinithmIO] Chart data file missing: {chartFilePath}");
        return new ChartData();
      }

      // Load chart data
      ChartData data = new ChartData()
      {
        SongMetaData = songMetaData
      };
      WNCParser.Parse(chartFilePath, data);

      return data;
    }

    public static ChartData LoadLevel(string levelDir, SongMetaData songMetaData, string chartID)
    {
      var file = new File();
      string chartFilePath = levelDir.PlusFile(songMetaData.ID).PlusFile(chartID + CHART_DATA_FILE);

      if (!file.FileExists(chartFilePath))
      {
        GD.PushError($"[WinithmIO] Chart data file missing: {chartFilePath}");
        return new ChartData();
      }

      ChartData data = new ChartData
      {
        SongMetaData = songMetaData
      };
      WNCParser.Parse(chartFilePath, data);

      return data;
    }

    /// <summary>Saves metadata and chart files to disk, ensuring directory existence.</summary>
    public static void SaveLevel(string levelsDir, ChartData data)
    {
      Directory dir = new Directory();
      string songDir = levelsDir.PlusFile(data.SongMetaData.ID);
      if (!dir.DirExists(songDir))
      {
        dir.MakeDirRecursive(songDir);
      }

      WNMGenerator.Generate(
        songDir.PlusFile(CHART_METADATA_FILE),
        data.SongMetaData
      );
      WNCGenerator.Generate(
        songDir.PlusFile(data.ChartMetadata.ChartID + CHART_DATA_FILE),
        data
      );
    }
  }
}