using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Post-processing shader overlay from [OVERLAYS].
  /// Format: + <ID> <initParam1> <initParam2> ...
  /// </summary>
  public class OverlayData : IStoryboardable<string>
  {
    public string ID;
    public string Name;

    public string ShaderFile;
    public bool AffectsUI = false;

    public int Layer = 0;

    /// <summary>Shader uniform definitions, auto-scanned from .glsl</summary>
    public Dictionary<string, ShaderParamDef> ShaderParams { get; } = new Dictionary<string, ShaderParamDef>();

    public Dictionary<string, AnyValue> InitParams = new Dictionary<string, AnyValue>();

    public Storyboard<string> StoryboardEvents { get; set; }

  }
}
