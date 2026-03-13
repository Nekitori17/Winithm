using System.IO;
using Winithm.Core.Data;

namespace Winithm.Core.Common
{
    /// <summary>
    /// High-level I/O facade for loading and saving Winithm level data.
    /// Delegates to WNMParser/WNCParser for reading, WNMGenerator/WNCGenerator for writing.
    /// </summary>
    public static class WinithmChartIO
    {
        /// <summary>Load and parse both .wnm and .wnc from a level folder.</summary>
        public static ChartData LoadLevel(string folderPath, string chartFileName)
        {
            string wnmPath = FindWnmFile(folderPath);
            if (wnmPath == null)
            {
                Godot.GD.PushError($"[WinithmIO] No .wnm file found in: {folderPath}");
                return new ChartData();
            }

            ChartData data = WNMParser.Parse(wnmPath);

            string wncPath = Path.Combine(folderPath, chartFileName + ".wnc");
            if (!File.Exists(wncPath))
            {
                Godot.GD.PushError($"[WinithmIO] Chart file not found: {wncPath}");
                return data;
            }

            WNCParser.Parse(wncPath, data);
            return data;
        }

        /// <summary>Save both .wnm and .wnc to a level folder.</summary>
        public static void SaveLevel(string folderPath, string chartFileName, ChartData data)
        {
            string wnmPath = Path.Combine(folderPath, "metadata.wnm");
            WNMGenerator.Generate(wnmPath, data);

            string wncPath = Path.Combine(folderPath, chartFileName + ".wnc");
            WNCGenerator.Generate(wncPath, data);
        }

        private static string FindWnmFile(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return null;
            string[] files = Directory.GetFiles(folderPath, "*.wnm");
            return files.Length > 0 ? files[0] : null;
        }
    }
}
