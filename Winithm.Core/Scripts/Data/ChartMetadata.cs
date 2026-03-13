using System.Collections.Generic;

namespace Winithm.Core.Data
{
    /// <summary>
    /// Combined metadata from both .wnm and .wnc files.
    /// </summary>
    public class ChartMetadata
    {
        // From .wnm [METADATA]
        public string ID = "";
        public string Name = "";
        public string NameAlt = "";
        public string Artist = "";
        public string ArtistAlt = "";
        public string Tags = "";
        public float PreviewStart;
        public float PreviewEnd;

        // From .wnc [METADATA] or .wnm [CHARTS]
        public int Index;
        public string ChartName = "";
        public string Charter = "";
        public string Level = "";
        public float Constant;
    }

    /// <summary>
    /// Resource definitions from .wnm [RESOURCES].
    /// </summary>
    public class ResourceData
    {
        // Song
        public string SongPath = "";
        public List<BPMStop> BPMList = new List<BPMStop>();

        // Illustration
        public string IllustrationPath = "";
        public string Illustrator = "";
        public float IconCenterX;
        public float IconCenterY;
        public float IconSize = 1f;
    }

    /// <summary>
    /// Chart definition reference from .wnm [CHARTS].
    /// </summary>
    public class ChartReference
    {
        public string ID = "";
        public int Index;
        public string Name = "";
        public string Charter = "";
        public string Level = "";
        public float Constant;
    }
}
