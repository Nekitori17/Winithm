using System.Collections.Generic;

namespace Winithm.Core.Data
{
    /// <summary>
    /// Top-level container aggregating all parsed data from .wnm and .wnc files.
    /// </summary>
    public class ChartData
    {
        // From .wnm
        public ChartMetadata Metadata = new ChartMetadata();
        public ResourceData Resources = new ResourceData();
        public List<ChartReference> ChartReferences = new List<ChartReference>();

        // From .wnc
        public List<ComponentData> Components = new List<ComponentData>();
        public List<ThemeChannelData> ThemeChannels = new List<ThemeChannelData>();
        public List<GroupData> Groups = new List<GroupData>();
        public List<WindowData> Windows = new List<WindowData>();
        public List<OverlayData> Overlays = new List<OverlayData>();
    }
}
