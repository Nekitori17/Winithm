using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages WindowData collections and monitors nested sub-manager (SpeedStep, Note, Storyboard) changes.
  /// </summary>
  public class WindowManager
  {
    public event Action<WindowManager> OnWindowChanged;

    public Metronome Metronome { get; private set; }
    public Dictionary<string, WindowData> WindowCollection { get; private set; } = new Dictionary<string, WindowData>();

    /// <summary>Windows sorted by StartBeat for cursor-based culling.</summary>
    public List<WindowData> SortedWindows { get; private set; } = new List<WindowData>();

    /// <summary>Prefix-max of EndBeatEndOut over SortedWindows, for backward sync binary search.</summary>
    public double[] MaxEndBeats { get; private set; } = Array.Empty<double>();

    private int _updateLockCount = 0;
    private bool _needsRecompute = false;

    public void BeginUpdate() => _updateLockCount++;

    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success)
      {
        CommitRecompute();
        OnWindowChanged?.Invoke(this);
      }
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0)
      {
        CommitRecompute();
        OnWindowChanged?.Invoke(this);
      }
    }

    private void RequestRecompute()
    {
      _needsRecompute = true;
    }

    private void CommitRecompute()
    {
      if (_needsRecompute)
      {
        Compute();
        _needsRecompute = false;
      }
    }

    /// <summary>
    /// Full recompute: rebuilds SortedWindows and MaxEndBeats from scratch.
    /// Cost is O(n log n) for sort + O(n) for prefix-max. Intended to be called
    /// infrequently (on add/remove, lifecycle change, or unresponsive state change).
    /// </summary>
    public void Compute()
    {
      SortedWindows.Clear();
      SortedWindows.AddRange(WindowCollection.Values);
      SortedWindows.Sort((a, b) => a.StartBeat.AbsoluteValue.CompareTo(b.StartBeat.AbsoluteValue));

      MaxEndBeats = new double[SortedWindows.Count];
      double runningMax = double.MinValue;
      for (int i = 0; i < SortedWindows.Count; i++)
      {
        runningMax = Math.Max(runningMax, SortedWindows[i].EndBeatEndOut);
        MaxEndBeats[i] = runningMax;
      }
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

      RequestRecompute();
      CommitRecompute();
    }

    // ==========================================
    // Event Subscription
    // ==========================================

    private void SubscribeChangeEvent(WindowData windowData)
    {
      windowData.OnUpdated -= HandleUpdated;
      windowData.OnUpdated += HandleUpdated;

      windowData.OnLifeCycleChanged -= HandleLifeCycleChanged;
      windowData.OnLifeCycleChanged += HandleLifeCycleChanged;

      windowData.OnUnFocusChanged -= HandleUnFocusChanged;
      windowData.OnUnFocusChanged += HandleUnFocusChanged;

      windowData.OnUnResponsiveChanged -= HandleUnResponsiveChanged;
      windowData.OnUnResponsiveChanged += HandleUnResponsiveChanged;
    }

    private void UnsubscribeChangeEvent(WindowData windowData)
    {
      windowData.OnUpdated -= HandleUpdated;
      windowData.OnLifeCycleChanged -= HandleLifeCycleChanged;
      windowData.OnUnFocusChanged -= HandleUnFocusChanged;
      windowData.OnUnResponsiveChanged -= HandleUnResponsiveChanged;
    }

    /// <summary>
    /// WindowData.OnDataChanged already aggregates events from its inner SpeedStep, Note, and Storyboard.
    /// A single subscription here captures all nested changes without redundant wiring.
    /// </summary>
    private void HandleUpdated(WindowData windowData) => NotifyChanged();
    private void HandleUnFocusChanged(WindowData windowData)
    {
      windowData.Notes.Compute();

      NotifyChanged();
    }
    private void HandleUnResponsiveChanged(WindowData windowData)
    {
      ComputeAnimations(windowData);

      RequestRecompute();
      NotifyChanged();
    }
    private void HandleLifeCycleChanged(WindowData windowData)
    {
      windowData.Notes.Compute();
      ComputeAnimations(windowData);

      RequestRecompute();
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

      RequestRecompute();
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

      RequestRecompute();
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
