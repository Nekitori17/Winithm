using System;
using Winithm.Core.Common;
using Winithm.Core.Interfaces;
using Winithm.Core.Managers;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Shared color palette from [THEME_CHANNELS].
  /// Format: + <ID> <initR> <initG> <initB> <initA> <initNoteA>
  /// </summary>
  public class ThemeChannelData : IStoryboardable<StoryboardProperty>, IDeepCloneable<ThemeChannelData>
  {
    public event Action<ThemeChannelData> OnDataChanged;

    public string ID;
    private string _name;
    public string Name { get => _name; set { if (_name == value) return; _name = value; OnDataChanged?.Invoke(this); } }

    private float _initR = 0f;
    public float InitR { get => _initR; set { if (_initR == value) return; _initR = value; OnDataChanged?.Invoke(this); } }

    private float _initG = 0f;
    public float InitG { get => _initG; set { if (_initG == value) return; _initG = value; OnDataChanged?.Invoke(this); } }

    private float _initB = 0f;
    public float InitB { get => _initB; set { if (_initB == value) return; _initB = value; OnDataChanged?.Invoke(this); } }

    private float _initA = 1f;
    public float InitA { get => _initA; set { if (_initA == value) return; _initA = value; OnDataChanged?.Invoke(this); } }

    private float _initNoteA = 1f;
    public float InitNoteA { get => _initNoteA; set { if (_initNoteA == value) return; _initNoteA = value; OnDataChanged?.Invoke(this); } }

    public Storyboard<StoryboardProperty> StoryboardEvents { get; set; } = new Storyboard<StoryboardProperty>();

    public ThemeChannelData()
    {
      StoryboardEvents.OnStoryboardChanged += BubbleStoryboard;
    }

    public ThemeChannelData DeepClone(BeatTime? offset)
    {
      var cloned = new ThemeChannelData();

      // Detach bubbling from the default StoryboardEvents created by constructor
      cloned.StoryboardEvents.OnStoryboardChanged -= cloned.BubbleStoryboard;

      cloned.ID = ID;
      cloned.Name = Name;
      cloned.InitR = InitR;
      cloned.InitG = InitG;
      cloned.InitB = InitB;
      cloned.InitA = InitA;
      cloned.InitNoteA = InitNoteA;
      cloned.StoryboardEvents = StoryboardEvents?.DeepClone(offset);

      // Re-wire bubbling to the cloned StoryboardEvents
      cloned.StoryboardEvents.OnStoryboardChanged += cloned.BubbleStoryboard;

      return cloned;
    }

    public static ThemeChannelData Parse(string text)
    {
      var current = new ThemeChannelData();

      string[] parts = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length >= 1) current.ID = parts[0];
      if (parts.Length >= 2) current.InitR =
        ParserUtils.TryParseFloat(parts[1], out float r) ? r : 0f;
      if (parts.Length >= 3) current.InitG =
        ParserUtils.TryParseFloat(parts[2], out float g) ? g : 0f;
      if (parts.Length >= 4) current.InitB =
        ParserUtils.TryParseFloat(parts[3], out float b) ? b : 0f;
      if (parts.Length >= 5) current.InitA =
        ParserUtils.TryParseFloat(parts[4], out float a) ? a : 1f;
      if (parts.Length >= 6) current.InitNoteA =
        ParserUtils.TryParseFloat(parts[5], out float na) ? na : 1f;

      return current;
    }

    // Named delegate for clean subscribe/unsubscribe in DeepClone
    private void BubbleStoryboard(Storyboard<StoryboardProperty> sb) => OnDataChanged?.Invoke(this);
  }
}
