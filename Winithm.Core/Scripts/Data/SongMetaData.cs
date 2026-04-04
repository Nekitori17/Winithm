using System.Collections.Generic;

namespace Winithm.Core.Data
{
  public class SongMetaData
  {
    // From .wnm [METADATA]
    public string ID;
    public string Name;
    public string NameAlt;
    public string Artist;
    public string ArtistAlt;
    public string Tags;
    public float PreviewStart;
    public float PreviewEnd;

    public ResourceData Resources;
    public List<ChartReference> Charts;
  }

  /// <summary>
  /// Resource definitions from .wnm [RESOURCES].
  /// </summary>
  public class ResourceData
  {
    // Song
    public string SongPath;
    public BaseBPM BaseBPM;
    public List<BPMStop> BPMList = new List<BPMStop>();

    // Illustration
    public string IllustrationPath;
    public string Illustrator;
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
    public int Index;
    public string Name;
    public string Charter;
    public string Level;
    public float Constant;
  }
}