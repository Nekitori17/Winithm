using System.Collections.Generic;

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
  public class ComponentData
  {
    public ComponentType Type;
    public float InitX;
    public float InitY;
    public float InitScale;
    public float InitAlpha;
    public float AnchorX;
    public float AnchorY;

    public List<StoryboardEvent> Events = new List<StoryboardEvent>();

    public static ComponentType ParseType(string text)
    {
      switch (text.Trim())
      {
        case "Combo": return ComponentType.Combo;
        case "Score": return ComponentType.Score;
        case "Info": return ComponentType.Info;
        case "Difficulty": return ComponentType.Difficulty;
        default:
          Godot.GD.PushWarning($"[WinithmParser] Unknown component type: '{text}', defaulting to Info.");
          return ComponentType.Info;
      }
    }
  }
}
