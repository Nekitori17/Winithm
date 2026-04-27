using System.Collections.Generic;
using Godot;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  [Tool]
  public class ComponentController : Control
  {
    public ComponentManager ComponentManager { get; private set; }

    private readonly Dictionary<ComponentType, double> _lastUpdateBeat = new Dictionary<ComponentType, double>();

    public ComponentController(ComponentManager manager)
    {
      ComponentManager = manager;

      foreach (var key in ComponentManager.Keys)
      {
        _lastUpdateBeat[key] = -1;
      }
    }

    public void Update(double currentBeat)
    {
      
    }

    public void ForceUpdate(double currentBeat, bool _force = true)
    {
      
    }
  }
}