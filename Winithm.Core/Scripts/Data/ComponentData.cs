using System;
using Winithm.Core.Interfaces;
using Winithm.Core.Managers;

namespace Winithm.Core.Data
{
  /// <summary>
  /// HUD component data with optional storyboard animations.
  /// </summary>
  public class ComponentData : IStoryboardable<StoryboardProperty>
  {
    public event Action<ComponentData> OnUpdated;
    private float _initY = 0f;
    public float InitY { get => _initY; set { if (_initY == value) return; _initY = value; OnUpdated?.Invoke(this); } }
    private float _initScale = 1f;
    public float InitScale { get => _initScale; set { if (_initScale == value) return; _initScale = value; OnUpdated?.Invoke(this); } }
    private float _initAlpha = 1f;
    public float InitAlpha { get => _initAlpha; set { if (_initAlpha == value) return; _initAlpha = value; OnUpdated?.Invoke(this); } }
    public StoryboardManager<StoryboardProperty> StoryboardEvents { get; set; } = new StoryboardManager<StoryboardProperty>();

    public ComponentData()
    {
      StoryboardEvents.OnUpdated += (sb) => OnUpdated?.Invoke(this);
    }
  }
}
