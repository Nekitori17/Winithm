using System;
using Winithm.Core.Managers;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Top-level container aggregating all parsed data from .wnm and .wnc files.
  /// </summary>
  public class ChartData
  {
    public event Action<ChartData> OnSongMetaDataUpdated;
    public event Action<ChartData> OnChartMetadataUpdated;

    public event Action<ChartData> OnChartUpdated;

    // Metadata
    public SongMetaData SongMetaData = new SongMetaData();
    public ChartMetadata ChartMetadata = new ChartMetadata();

    // Contents
    public OverlayManager Overlays = new OverlayManager();
    public ComponentManager Components = new ComponentManager();
    public ThemeChannelManager ThemeChannels = new ThemeChannelManager();
    public GroupManager Groups = new GroupManager();
    public WindowManager Windows = new WindowManager();

    public ObjectFactory ObjectFactory = new ObjectFactory();

    public ChartData()
    {
      SongMetaData.OnUpdated += (sm) => OnSongMetaDataUpdated?.Invoke(this);
      ChartMetadata.OnUpdated += (cm) => OnChartMetadataUpdated?.Invoke(this);

      Overlays.OnUpdated += (om) => OnChartUpdated?.Invoke(this);
      Components.OnUpdated += (cmp) => OnChartUpdated?.Invoke(this);
      ThemeChannels.OnUpdated += (tc) => OnChartUpdated?.Invoke(this);
      Groups.OnUpdated += (g) => OnChartUpdated?.Invoke(this);
      Windows.OnUpdated += (wm) => OnChartUpdated?.Invoke(this);
    }
  }
}
