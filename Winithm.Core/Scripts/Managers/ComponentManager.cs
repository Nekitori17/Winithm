using System;
using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.Managers
{
  public enum ComponentType
  {
    Combo,
    Score,
    Info,
    Difficulty
  }

  /// <summary>
  /// Manages active component data and tracks property changes.
  /// </summary>
  public class ComponentManager
  {
    public event Action<ComponentManager> OnUpdated;

    public Dictionary<ComponentType, ComponentData> ComponentDictionary { get; private set; } = new Dictionary<ComponentType, ComponentData>();

    public ComponentManager()
    {
      BeginUpdate();
      SetComponent(ComponentType.Info, new ComponentData());
      SetComponent(ComponentType.Difficulty, new ComponentData());
      SetComponent(ComponentType.Combo, new ComponentData());
      SetComponent(ComponentType.Score, new ComponentData());
      EndUpdate();
    }

    private int _updateLockCount = 0;

    public void BeginUpdate() => _updateLockCount++;

    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success) OnUpdated?.Invoke(this);
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0) OnUpdated?.Invoke(this);
    }

    private void SubscribeChangeEvent(ComponentData componentData)
    {
      componentData.OnUpdated -= HandleUpdated;
      componentData.OnUpdated += HandleUpdated;
    }

    private void UnsubscribeChangeEvent(ComponentData componentData)
    {
      componentData.OnUpdated -= HandleUpdated;
    }

    private void HandleUpdated(ComponentData componentData) => NotifyChanged();

    public void SetComponent(ComponentType type, ComponentData compData)
    {
      if (ComponentDictionary.TryGetValue(type, out var comp))
      {
        UnsubscribeChangeEvent(comp);
      }

      ComponentDictionary[type] = compData;
      SubscribeChangeEvent(compData);

      NotifyChanged();
    }

    public ComponentData GetComponent(ComponentType type) => ComponentDictionary[type];
  }
}