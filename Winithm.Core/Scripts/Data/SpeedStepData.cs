using System;
using Winithm.Core.Common;
using Winithm.Core.Interfaces;
using Winithm.Core.Managers;

namespace Winithm.Core.Data
{
  /// <summary>
  /// Defines scrolling velocity at a given beat and window lifecycle boundaries.
  /// </summary>
  public class SpeedStepData : IStoryboardable<StoryboardProperty>, IDeepCloneable<SpeedStepData>
  {
    public event Action<SpeedStepData> OnStartBeatChanged;
    public event Action<SpeedStepData> OnDataChanged;

    public string ID;

    private BeatTime _startBeat = BeatTime.NaN;
    public BeatTime StartBeat { get => _startBeat; set { if (_startBeat != value) { _startBeat = value; OnStartBeatChanged?.Invoke(this); } } }

    private float _multiplier = 1f;
    public float Multiplier { get => _multiplier; set { if (_multiplier != value) { _multiplier = value; OnDataChanged?.Invoke(this); } } }

    public StoryboardManager<StoryboardProperty> StoryboardEvents { get; set; } = new StoryboardManager<StoryboardProperty>();

    public SpeedStepData()
    {
      StoryboardEvents.OnStoryboardChanged += BubbleStoryboard;
    }

    public SpeedStepData DeepClone(BeatTime? offset)
    {
      var cloned = new SpeedStepData();

      // Detach bubbling from the default StoryboardEvents created by constructor
      cloned.StoryboardEvents.OnStoryboardChanged -= cloned.BubbleStoryboard;

      cloned.ID = ID;
      cloned.StartBeat = StartBeat + (offset ?? BeatTime.Zero);
      cloned.Multiplier = Multiplier;
      cloned.StoryboardEvents = StoryboardEvents?.DeepClone(offset);

      // Re-wire bubbling to the cloned StoryboardEvents
      cloned.StoryboardEvents.OnStoryboardChanged += cloned.BubbleStoryboard;

      return cloned;
    }

    // Named delegate for clean subscribe/unsubscribe in DeepClone
    private void BubbleStoryboard(StoryboardManager<StoryboardProperty> sb) => OnDataChanged?.Invoke(this);

    public static SpeedStepData Parse(string text)
    {
      var current = new SpeedStepData();

      var parts = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

      if (parts.Length >= 1) current.ID = parts[0];
      if (parts.Length >= 2) current.StartBeat =
        BeatTime.TryParse(parts[1], out BeatTime startBeat) ? startBeat : BeatTime.Zero;
      if (parts.Length >= 3) current.Multiplier =
        ParserUtils.TryParseFloat(parts[2], out float multiplier) ? multiplier : 1f;

      return current;
    }

    public override string ToString() => $"{ID} {StartBeat} {Multiplier}";
  }
}
