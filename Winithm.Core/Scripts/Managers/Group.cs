using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages GroupData configurations and tracks underlying data changes.
  /// Storyboard changes are already bubbled through GroupData.OnDataChanged.
  /// </summary>
  public class Group : IDeepCloneable<Group>
  {
    public event Action<Group> OnGroupChanged;

    public Dictionary<string, GroupData> GroupCollection { get; private set; } = new Dictionary<string, GroupData>();

    private int _updateLockCount = 0;

    public void BeginUpdate() => _updateLockCount++;

    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success) OnGroupChanged?.Invoke(this);
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0) OnGroupChanged?.Invoke(this);
    }

    public Group DeepClone(BeatTime? offset)
    {
      var cloned = new Group();

      cloned.BeginUpdate();
      foreach (var group in GroupCollection.Values)
        cloned.AddGroup(group.DeepClone(offset));
      cloned.EndUpdate();

      return cloned;
    }

    // GroupData.OnDataChanged already aggregates StoryboardEvents changes.
    // Single subscription avoids double-firing.
    private void SubscribeChangeEvent(GroupData groupData)
    {
      groupData.OnDataChanged -= HandleDataChanged;
      groupData.OnDataChanged += HandleDataChanged;
    }

    private void UnsubscribeChangeEvent(GroupData groupData)
    {
      groupData.OnDataChanged -= HandleDataChanged;
    }

    private void HandleDataChanged(GroupData groupData) => NotifyChanged();

    public void AddGroup(GroupData groupData)
    {
      if (string.IsNullOrEmpty(groupData.ID))
        throw new ArgumentException("GroupData ID cannot be null or empty.");

      // Overwrite and cleanly strip old event bindings if ID collision occurs
      if (GroupCollection.TryGetValue(groupData.ID, out var existing))
        UnsubscribeChangeEvent(existing);

      GroupCollection[groupData.ID] = groupData;
      SubscribeChangeEvent(groupData);
      NotifyChanged();
    }

    public void AddGroups(List<GroupData> groups)
    {
      if (groups.Count == 0) return;

      BeginUpdate();
      foreach (var group in groups) AddGroup(group);
      EndUpdate();
    }

    public bool RemoveGroup(string id)
    {
      if (!GroupCollection.TryGetValue(id, out var groupData)) return false;

      UnsubscribeChangeEvent(groupData);
      GroupCollection.Remove(id);
      NotifyChanged();

      return true;
    }

    public int RemoveGroups(List<string> ids)
    {
      if (ids.Count == 0) return 0;

      BeginUpdate();
      int success = ids.Count(id => RemoveGroup(id));
      EndUpdate(success > 0);

      return success;
    }

    public GroupData GetGroup(string id)
    {
      if (GroupCollection.TryGetValue(id, out var groupData)) return groupData;
      throw new KeyNotFoundException($"Group {id} not found.");
    }

    public List<GroupData> GetGroups(List<string> ids)
    {
      var result = new List<GroupData>();
      foreach (var id in ids)
      {
        if (GroupCollection.TryGetValue(id, out var group)) result.Add(group);
      }
      return result;
    }
    
    public bool ContainsGroup(string id) => GroupCollection.ContainsKey(id);

    public List<GroupData> GetAllGroups() => GroupCollection.Values.ToList();
  }
}
