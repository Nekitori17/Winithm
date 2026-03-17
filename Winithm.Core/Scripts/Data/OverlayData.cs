using System.Collections.Generic;
using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Post-processing shader overlay from [OVERLAYS].
  /// Format: + <ID> <initParam1> <initParam2> ...
  /// </summary>
  public class OverlayData : IStoryboardTarget<string>
  {
    public string ID = "";
    public string Name = "";

    public string ShaderFile = "";
    public bool AffectsUI = false;
    
    public Dictionary<string, PropertyDef> PropertyRegistry { get; } = new Dictionary<string, PropertyDef>();
    
    /// <summary>Deeper metadata for shader params (ColorRGB hint, etc.)</summary>
    public Dictionary<string, PropertyDef> ParamMetadata { get; set; } = new Dictionary<string, PropertyDef>();
    
    public Dictionary<string, AnyValue> InitParams = new Dictionary<string, AnyValue>();
    
    public Dictionary<string, List<StoryboardEvent>> StoryboardEvents { get; set; }
    
  }
}
