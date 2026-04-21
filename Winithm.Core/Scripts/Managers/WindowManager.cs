using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages WindowData collections and monitors nested sub-manager (SpeedStep, Note, Storyboard) changes.
  /// </summary>
  public class WindowManager : IDeepCloneable<WindowManager>
  {
    public event Action<WindowManager> OnWindowChanged;

    public Metronome Metronome { get; private set; }
    public Dictionary<string, WindowData> WindowCollection { get; private set; } = new Dictionary<string, WindowData>();

    private int _updateLockCount = 0;

    public void BeginUpdate() => _updateLockCount++;

    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success) OnWindowChanged?.Invoke(this);
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0) OnWindowChanged?.Invoke(this);
    }

    public WindowManager DeepClone(BeatTime? offset)
    {
      var cloned = new WindowManager();
      cloned.SetMetronome(Metronome);

      cloned.BeginUpdate();
      foreach (var window in WindowCollection.Values)
        cloned.AddWindow(window.DeepClone(offset));
      cloned.EndUpdate();

      return cloned;
    }

    public void SetMetronome(Metronome metronome)
    {
      Metronome = metronome;
    }

    public void ComputeAnimations(WindowData windowData)
    {
      if (Metronome == null)
        throw new InvalidOperationException("Metronome must be set before computing animations.");

      windowData.PreComputeAnimation(Metronome);

      if (windowData.Unresponsive)
        windowData.ComputeAnimationWhenUnresponsive(Metronome);
    }

    public void ComputeAllAnimations()
    {
      if (Metronome == null)
        throw new InvalidOperationException("Metronome must be set before computing animations.");

      foreach (var window in WindowCollection.Values)
        ComputeAnimations(window);
    }

    // ==========================================
    // Event Subscription
    // ==========================================

    private void SubscribeChangeEvent(WindowData windowData)
    {
      windowData.OnDataChanged -= HandleDataChanged;
      windowData.OnDataChanged += HandleDataChanged;

      windowData.OnLifeCycleChanged -= HandleLifeCycleChanged;
      windowData.OnLifeCycleChanged += HandleLifeCycleChanged;

      windowData.OnUnFocusChanged -= HandleUnFocusChanged;
      windowData.OnUnFocusChanged += HandleUnFocusChanged;
    }

    private void UnsubscribeChangeEvent(WindowData windowData)
    {
      windowData.OnDataChanged -= HandleDataChanged;
      windowData.OnLifeCycleChanged -= HandleLifeCycleChanged;
      windowData.OnUnFocusChanged -= HandleUnFocusChanged;
    }

    /// <summary>
    /// WindowData.OnDataChanged already aggregates events from its inner SpeedStep, Note, and Storyboard.
    /// A single subscription here captures all nested changes without redundant wiring.
    /// </summary>
    private void HandleDataChanged(WindowData windowData) => NotifyChanged();
    private void HandleUnFocusChanged(WindowData windowData) {
      windowData.Notes.Compute();

      NotifyChanged();
    }
    private void HandleLifeCycleChanged(WindowData windowData) {
      windowData.Notes.Compute();
      ComputeAnimations(windowData);

      NotifyChanged();
    }

    // ==========================================
    // Lifecycle Management
    // ==========================================

    public void AddWindow(WindowData windowData)
    {
      if (string.IsNullOrEmpty(windowData.ID))
        throw new ArgumentException("WindowData ID cannot be null or empty.");

      // Overwrite: cleanly strip old event bindings if ID collision occurs
      if (WindowCollection.TryGetValue(windowData.ID, out var existing))
        UnsubscribeChangeEvent(existing);

      WindowCollection[windowData.ID] = windowData;
      SubscribeChangeEvent(windowData);
      NotifyChanged();
    }

    public void AddWindows(List<WindowData> windows)
    {
      if (windows.Count == 0) return;

      BeginUpdate();
      foreach (var window in windows) AddWindow(window);
      EndUpdate();
    }

    public bool RemoveWindow(string id)
    {
      if (!WindowCollection.TryGetValue(id, out var windowData)) return false;

      UnsubscribeChangeEvent(windowData);
      WindowCollection.Remove(id);
      NotifyChanged();

      return true;
    }

    public int RemoveWindows(List<string> ids)
    {
      if (ids.Count == 0) return 0;

      BeginUpdate();
      int success = ids.Count(id => RemoveWindow(id));
      EndUpdate(success > 0);

      return success;
    }

    // ==========================================
    // Fetch Methods
    // ==========================================

    public WindowData GetWindow(string id)
    {
      if (WindowCollection.TryGetValue(id, out var windowData)) return windowData;
      throw new KeyNotFoundException($"Window {id} not found.");
    }

    public List<WindowData> GetWindows(List<string> ids)
    {
      var result = new List<WindowData>();
      foreach (var id in ids)
      {
        if (WindowCollection.TryGetValue(id, out var window)) result.Add(window);
      }
      return result;
    }

    public List<WindowData> GetAllWindows() => WindowCollection.Values.ToList();

    /// <summary>
    /// Returns all windows sorted by Layer (ascending) for correct render order.
    /// </summary>
    public List<WindowData> GetWindowsByLayer()
    {
      var sorted = WindowCollection.Values.ToList();
      sorted.Sort((a, b) => a.Layer.CompareTo(b.Layer));
      return sorted;
    }
  }
}
