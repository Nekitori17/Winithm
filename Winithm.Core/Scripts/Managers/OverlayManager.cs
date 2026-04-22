using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Data;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages OverlayData configurations and tracks underlying data changes.
  /// Storyboard changes are already bubbled through OverlayData.OnUpdated.
  /// </summary>
  public class OverlayManager
  {
    public event Action<OverlayManager> OnUpdated;

    public Dictionary<string, OverlayData> OverlayCollection { get; private set; } = new Dictionary<string, OverlayData>();

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

    private void SubscribeChangeEvent(OverlayData overlayData)
    {
      overlayData.OnUpdated -= HandleUpdated;
      overlayData.OnUpdated += HandleUpdated;
    }

    private void UnsubscribeChangeEvent(OverlayData overlayData)
    {
      overlayData.OnUpdated -= HandleUpdated;
    }

    private void HandleUpdated(OverlayData overlayData) => NotifyChanged();

    public void AddOverlay(OverlayData overlayData)
    {
      if (string.IsNullOrEmpty(overlayData.ID))
        throw new ArgumentException("OverlayData ID cannot be null or empty.");

      if (OverlayCollection.TryGetValue(overlayData.ID, out var existing))
        UnsubscribeChangeEvent(existing);

      OverlayCollection[overlayData.ID] = overlayData;
      SubscribeChangeEvent(overlayData);
      NotifyChanged();
    }

    public void AddOverlays(List<OverlayData> overlays)
    {
      if (overlays.Count == 0) return;

      BeginUpdate();
      foreach (var overlay in overlays) AddOverlay(overlay);
      EndUpdate();
    }

    public bool RemoveOverlay(string id)
    {
      if (!OverlayCollection.TryGetValue(id, out var overlayData)) return false;

      UnsubscribeChangeEvent(overlayData);
      OverlayCollection.Remove(id);
      NotifyChanged();

      return true;
    }

    public int RemoveOverlays(List<string> ids)
    {
      if (ids.Count == 0) return 0;

      BeginUpdate();
      int success = ids.Count(id => RemoveOverlay(id));
      EndUpdate(success > 0);

      return success;
    }

    public OverlayData GetOverlay(string id)
    {
      if (OverlayCollection.TryGetValue(id, out var overlayData)) return overlayData;
      throw new KeyNotFoundException($"Overlay {id} not found.");
    }

    public List<OverlayData> GetOverlays(List<string> ids)
    {
      var result = new List<OverlayData>();
      foreach (var id in ids)
      {
        if (OverlayCollection.TryGetValue(id, out var overlay)) result.Add(overlay);
      }
      return result;
    }

    public bool ContainsOverlay(string id) => OverlayCollection.ContainsKey(id);

    public List<OverlayData> GetAllOverlays() => OverlayCollection.Values.ToList();
  }
}
