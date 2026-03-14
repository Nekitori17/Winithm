using System.Collections.Generic;
using Winithm.Core.Common;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Post-processing shader overlay from [OVERLAYS].
  /// Format: + <ID> <initParam1> <initParam2> ...
  /// </summary>
  public class OverlayData
  {
    public string ID = "";
    public string Name = "";
    public string ShaderFile = "";
    public List<VectorValue> InitParams;

    public List<StoryboardEvent> Events;

    public OverlayData()
    {
      InitParams = new List<VectorValue>();
      Events = new List<StoryboardEvent>();
    }
  }
}
