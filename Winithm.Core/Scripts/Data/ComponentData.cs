using System.Collections.Generic;
using System.Diagnostics;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Data
{
  public enum ComponentType
  {
    Combo,
    Score,
    Info,
    Difficulty
  }

  /// <summary>
  /// HUD component definition from [COMPONENTS].
  /// Format: * <Type> <initX> <initY> <initScale> <initAlpha> <anchorX> <anchorY>
  /// </summary>
  public class ComponentData : IStoryboardTarget<StoryboardProperty>
  {
    public ComponentType Type;

    public float InitX = 0f;
    public float InitY = 0f;
    public float InitScale = 1f;
    public float InitAlpha = 1f;
    public float AnchorX = 0.5f;
    public float AnchorY = 0.5f;

    public Dictionary<StoryboardProperty, List<StoryboardEvent>> StoryboardEvents { get; set; }

    public static ComponentType ParseType(string text)
    {
      switch (text.Trim())
      {
        case "Combo": return ComponentType.Combo;
        case "Score": return ComponentType.Score;
        case "Info": return ComponentType.Info;
        case "Difficulty": return ComponentType.Difficulty;
        default:
          Trace.TraceWarning($"[WinithmParser] Unknown component type: '{text}', defaulting to Info.");
          return ComponentType.Info;
      }
    }
  }
}
