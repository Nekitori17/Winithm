using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages ThemeChannelData configurations and monitors inner data changes.
  /// Storyboard changes are already bubbled through ThemeChannelData.OnDataChanged.
  /// </summary>
  public class ThemeChannelManager : IDeepCloneable<ThemeChannelManager>
  {
    public event Action<ThemeChannelManager> OnThemeChannelChanged;

    public Dictionary<string, ThemeChannelData> ThemeChannelCollection { get; private set; } = new Dictionary<string, ThemeChannelData>();

    private int _updateLockCount = 0;

    public void BeginUpdate() => _updateLockCount++;

    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success) OnThemeChannelChanged?.Invoke(this);
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0) OnThemeChannelChanged?.Invoke(this);
    }

    public ThemeChannelManager DeepClone(BeatTime? offset)
    {
      var cloned = new ThemeChannelManager();

      cloned.BeginUpdate();
      foreach (var theme in ThemeChannelCollection.Values)
        cloned.AddThemeChannel(theme.DeepClone(offset));
      cloned.EndUpdate();

      return cloned;
    }

    // ThemeChannelData.OnDataChanged already aggregates StoryboardEvents changes.
    // Single subscription avoids double-firing.
    private void SubscribeChangeEvent(ThemeChannelData themeChannelData)
    {
      themeChannelData.OnDataChanged -= HandleDataChanged;
      themeChannelData.OnDataChanged += HandleDataChanged;
    }

    private void UnsubscribeChangeEvent(ThemeChannelData themeChannelData)
    {
      themeChannelData.OnDataChanged -= HandleDataChanged;
    }

    private void HandleDataChanged(ThemeChannelData themeChannelData) => NotifyChanged();

    public void AddThemeChannel(ThemeChannelData themeChannelData)
    {
      if (string.IsNullOrEmpty(themeChannelData.ID))
        throw new ArgumentException("ThemeChannelData ID cannot be null or empty.");

      if (ThemeChannelCollection.TryGetValue(themeChannelData.ID, out var existing))
        UnsubscribeChangeEvent(existing);

      ThemeChannelCollection[themeChannelData.ID] = themeChannelData;
      SubscribeChangeEvent(themeChannelData);
      NotifyChanged();
    }

    public void AddThemeChannels(List<ThemeChannelData> themeChannels)
    {
      if (themeChannels.Count == 0) return;

      BeginUpdate();
      foreach (var channel in themeChannels) AddThemeChannel(channel);
      EndUpdate();
    }

    public bool RemoveThemeChannel(string id)
    {
      if (!ThemeChannelCollection.TryGetValue(id, out var channelData)) return false;

      UnsubscribeChangeEvent(channelData);
      ThemeChannelCollection.Remove(id);
      NotifyChanged();

      return true;
    }

    public int RemoveThemeChannels(List<string> ids)
    {
      if (ids.Count == 0) return 0;

      BeginUpdate();
      int success = ids.Count(id => RemoveThemeChannel(id));
      EndUpdate(success > 0);

      return success;
    }

    public ThemeChannelData GetThemeChannel(string id)
    {
      if (ThemeChannelCollection.TryGetValue(id, out var channelData)) return channelData;
      throw new KeyNotFoundException($"ThemeChannel {id} not found.");
    }

    public List<ThemeChannelData> GetThemeChannels(List<string> ids)
    {
      var result = new List<ThemeChannelData>();
      foreach (var id in ids)
      {
        if (ThemeChannelCollection.TryGetValue(id, out var channel)) result.Add(channel);
      }
      return result;
    }

    public bool ContainsThemeChannel(string id) => ThemeChannelCollection.ContainsKey(id);

    public List<ThemeChannelData> GetAllThemeChannels() => ThemeChannelCollection.Values.ToList();
  }
}
