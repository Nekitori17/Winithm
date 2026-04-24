using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Data;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages GroupData configurations and tracks data changes.
  /// </summary>
  public class GroupManager
  {
    public event Action<GroupManager> OnUpdated;

    public Dictionary<string, GroupData> GroupCollection { get; private set; } = new Dictionary<string, GroupData>();

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

    private void SubscribeChangeEvent(GroupData groupData)
    {
      groupData.OnUpdated -= HandleUpdated;
      groupData.OnUpdated += HandleUpdated;
    }

    private void UnsubscribeChangeEvent(GroupData groupData)
    {
      groupData.OnUpdated -= HandleUpdated;
    }

    private void HandleUpdated(GroupData groupData) => NotifyChanged();

    public void AddGroup(GroupData groupData)
    {
      if (string.IsNullOrEmpty(groupData.ID)) return;

      if (GroupCollection.TryGetValue(groupData.ID, out var existing))
        UnsubscribeChangeEvent(existing);

      GroupCollection[groupData.ID] = groupData;
      SubscribeChangeEvent(groupData);
      NotifyChanged();
    }

    public void AddGroups(IEnumerable<GroupData> groups)
    {
      if (!groups.Any()) return;

      BeginUpdate();
      foreach (var group in groups) AddGroup(group);
      EndUpdate();
    }

    public bool RemoveGroup(string id)
    {
      if (string.IsNullOrEmpty(id)) return false;

      if (!GroupCollection.TryGetValue(id, out var groupData)) return false;

      UnsubscribeChangeEvent(groupData);
      GroupCollection.Remove(id);
      NotifyChanged();

      return true;
    }

    public int RemoveGroups(IEnumerable<string> ids)
    {
      if (!ids.Any()) return 0;

      BeginUpdate();
      int success = ids.Count(id => RemoveGroup(id));
      EndUpdate(success > 0);

      return success;
    }

    public GroupData GetGroup(string id)
    {
      if (string.IsNullOrEmpty(id)) return null;

      if (GroupCollection.TryGetValue(id, out var groupData)) return groupData;
        return null;
    }

    public IReadOnlyList<GroupData> GetGroups(IEnumerable<string> ids)
    {
      if (!ids.Any()) return Array.Empty<GroupData>();

      var result = new List<GroupData>();
      foreach (var id in ids)
      {
        var group = GetGroup(id);
        if (group != null) result.Add(group);
      }
      return result;
    }

    public bool ContainsGroup(string id) => GroupCollection.ContainsKey(id);

    public IReadOnlyDictionary<string, GroupData> GetAllGroups() => GroupCollection;
  }
}
