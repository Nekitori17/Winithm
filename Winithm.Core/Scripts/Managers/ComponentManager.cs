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

    /// <summary>
    /// Parses a component line from chart data.
    /// </summary>
    public static ComponentData ParseComponentLine(string text, out ComponentType type)
    {
      type = ComponentType.Info;
      string[] parts = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      var current = new ComponentData();

      if (parts.Length >= 1) 
        type = Enum.TryParse<ComponentType>(parts[0], out var t) ? t : ComponentType.Info;
      if (parts.Length >= 2) 
        current.InitX = ParserUtils.TryParseFloat(parts[1], out float x) ? x : 0f;
      if (parts.Length >= 3) 
        current.InitY = ParserUtils.TryParseFloat(parts[2], out float y) ? y : 0f;
      if (parts.Length >= 4) 
        current.InitScale = ParserUtils.TryParseFloat(parts[3], out float s) ? s : 1f;
      if (parts.Length >= 5) 
        current.InitAlpha = ParserUtils.TryParseFloat(parts[4], out float a) ? a : 1f;

      return current;
    }

    /// <summary>
    /// Generates a component line for chart data.
    /// </summary>
    public static string GenerateComponentLine(ComponentData comp, ComponentType type, int indent = 0)
    {
      return $"* {type} {comp.InitX} {comp.InitY} {comp.InitScale} {comp.InitAlpha}".PadLeft(indent);
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