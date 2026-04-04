using Godot;
using Winithm.Core.Data;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Facade for level I/O operations. 
  /// Handles directory traversal and file loading using Godot's virtual filesystem (res://, user://).
  /// </summary>
  public static class WinithmChartIO
  {
    /// <summary>Loads metadata (.wnm) and a specific chart (.wnc) from a level folder.</summary>
    public static ChartData LoadLevel(string folderPath, string chartFileName)
    {
      string wnmPath = FindWnmFile(folderPath);
      if (string.IsNullOrEmpty(wnmPath))
      {
        GD.PushError($"[WinithmIO] No .wnm metadata found in: {folderPath}");
        return new ChartData();
      }

      // Load shared chart metadata
      SongMetaData songMetaData = WNMParser.Parse(wnmPath);

      // Construct platform-safe path for the specific chart
      string wncPath = folderPath.PlusFile(chartFileName + ".wnc");

      ChartData data = new ChartData();
      WNCParser.Parse(wncPath, data);
      data.SongMetaData = songMetaData;

      File file = new File();
      if (!file.FileExists(wncPath))
      {
        GD.PushError($"[WinithmIO] Chart data file missing: {wncPath}");
        return data;
      }

      WNCParser.Parse(wncPath, data);
      return data;
    }

    /// <summary>Saves metadata and chart files to disk, ensuring directory existence.</summary>
    public static void SaveLevel(string folderPath, string chartFileName, ChartData data)
    {
      Directory dir = new Directory();
      if (!dir.DirExists(folderPath))
      {
        dir.MakeDirRecursive(folderPath);
      }

      WNMGenerator.Generate(folderPath.PlusFile("metadata.wnm"), data.SongMetaData);
      WNCGenerator.Generate(folderPath.PlusFile(chartFileName + ".wnc"), data);
    }


    /// <summary>Scans folder to find the first .wnm file using Godot's Directory API.</summary>
    private static string FindWnmFile(string folderPath)
    {
      Directory dir = new Directory();
      if (dir.Open(folderPath) != Error.Ok) return null;

      dir.ListDirBegin(skipNavigational: true);

      string fileName;
      while ((fileName = dir.GetNext()) != "")
      {
        if (!dir.CurrentIsDir() && fileName.EndsWith(".wnm"))
        {
          dir.ListDirEnd();
          return folderPath.PlusFile(fileName);
        }
      }

      dir.ListDirEnd();
      return null;
    }
  }
}