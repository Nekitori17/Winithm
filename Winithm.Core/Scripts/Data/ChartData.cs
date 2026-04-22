using Winithm.Core.Managers;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Top-level container aggregating all parsed data from .wnm and .wnc files.
  /// </summary>
  public class ChartData
  {
    // From .wnm
    public SongMetaData SongMetaData = new SongMetaData();
    public ChartMetadata Metadata = new ChartMetadata();

    // From .wnc
    // public List<ComponentData> Components = new List<ComponentData>();
    // public List<OverlayData> Overlays = new List<OverlayData>();
    public ThemeChannelManager ThemeChannels = new ThemeChannelManager();
    public GroupManager Groups = new GroupManager();
    public WindowManager Windows = new WindowManager();
  }
}
