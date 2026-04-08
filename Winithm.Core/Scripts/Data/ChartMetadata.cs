namespace Winithm.Core.Data
{
  /// <summary>
  /// Combined metadata from both .wnm and .wnc files.
  /// </summary>
  public class ChartMetadata
  {
    public SongMetaData SongMetaData = new SongMetaData();

    // From .wnc [METADATA] or .wnm [CHARTS]
    public int Index = 0;
    public string ChartID = "unnamed";
    public string ChartName = "Unamed";
    public string Charter = "Noname";
    public string Level = "1";
    public float Constant = 1f;
    public long NextIDSeed = 0;
  }
}
