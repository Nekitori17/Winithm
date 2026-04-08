using System.Collections.Generic;

namespace Winithm.Core.Data
{
  public class SongMetaData
  {
    // From .wnm [METADATA]
    public string ID;
    public string Name = "Unnamed";
    public string NameAlt;
    public string Artist = "Noname";
    public string ArtistAlt;
    public string Tags = "Genreless";
    public float PreviewStart = 0;
    public float PreviewEnd = 15;

    public ResourceData Resources = new ResourceData();
    public List<ChartReference> Charts = new List<ChartReference>();
  }

  /// <summary>
  /// Resource definitions from .wnm [RESOURCES].
  /// </summary>
  public class ResourceData
  {
    // Song
    public string SongPath;
    public BaseBPM BaseBPM = new BaseBPM(0, 120, 4);
    public List<BPMStop> BPMList = new List<BPMStop>();

    // Illustration
    public string IllustrationPath;
    public string Illustrator = "Noname";
    public float IconCenterX = 0.5f;
    public float IconCenterY = 0.5f;
    public float IconSize = 1f;
  }

  /// <summary>
  /// Chart definition reference from .wnm [CHARTS].
  /// </summary>
  public class ChartReference
  {
    public string ID;
    public int Index = 0;
    public string Name = "Unamed";
    public string Charter = "Noname";
    public string Level = "1";
    public float Constant = 1f;
  }
}