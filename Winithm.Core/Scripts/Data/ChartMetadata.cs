using System.Collections.Generic;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Combined metadata from both .wnm and .wnc files.
  /// </summary>
  public class ChartMetadata
  {
    public SongMetaData SongMetaData;

    // From .wnc [METADATA] or .wnm [CHARTS]
    public int Index;
    public string ChartID;
    public string ChartName;
    public string Charter;
    public string Level;
    public float Constant;
    public long NextIDSeed;
  }
}
