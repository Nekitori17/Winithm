using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Data;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages WindowData collections and monitors nested sub-manager changes.
  /// </summary>
  public class WindowManager
  {
    public event Action<WindowManager> OnUpdated;

    public Metronome Metronome { get; private set; }

    /// <summary>
    /// Collection of windows sorted by StartBeat.
    /// </summary>
    public List<WindowData> WindowCollection { get; private set; } = new List<WindowData>();

    /// <summary>
    /// Prefix-max of EndBeatEndOut over windows for binary search.
    /// </summary>
    public double[] MaxEndBeats { get; private set; } = Array.Empty<double>();

    /// <summary>
    /// Prefix sum of TotalComboCount for windows.
    /// </summary>
    public int[] PrefixCombo { get; private set; } = Array.Empty<int>();

    /// <summary>
    /// Total combo count across all windows.
    /// </summary>
    public int TotalComboCount { get; private set; } = 0;

    private int _updateLockCount = 0;
    private bool _needsRecompute = false;

    /// <summary>
    /// Suspends notifications to allow batch edits.
    /// </summary>
    public void BeginUpdate() => _updateLockCount++;

    /// <summary>
    /// Resumes notifications and runs Compute if edits were made.
    /// </summary>
    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success)
      {
        CommitRecompute();
        OnUpdated?.Invoke(this);
      }
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0)
      {
        CommitRecompute();
        OnUpdated?.Invoke(this);
      }
    }

    private void RequestRecompute() => _needsRecompute = true;

    private void CommitRecompute()
    {
      if (_needsRecompute)
      {
        Compute();
        _needsRecompute = false;
      }
    }

    /// <summary>
    /// Rebuilds MaxEndBeats and PrefixCombo based on the current WindowCollection.
    /// </summary>
    public void Compute()
    {
      MaxEndBeats = new double[WindowCollection.Count];
      PrefixCombo = new int[WindowCollection.Count];
      
      double runningMax = double.MinValue;
      int runningCombo = 0;

      for (int i = 0; i < WindowCollection.Count; i++)
      {
        var window = WindowCollection[i];
        
        runningMax = Math.Max(runningMax, window.EndBeatEndOut);
        MaxEndBeats[i] = runningMax;

        runningCombo += window.Notes.TotalComboCount;
        PrefixCombo[i] = runningCombo;
      }

      TotalComboCount = runningCombo;
    }

    public void SetMetronome(Metronome metronome)
    {
      if (Metronome == metronome) return;

      if (Metronome != null) Metronome.OnUpdated -= HandleMetronomeUpdated;
      Metronome = metronome;
      if (Metronome != null) Metronome.OnUpdated += HandleMetronomeUpdated;
      NotifyChanged();
    }

    private void HandleMetronomeUpdated(Metronome metronome)
    {
      RequestRecompute();
      NotifyChanged();
    }

    /// <summary>
    /// Computes animation data for a single window.
    /// </summary>
    public void ComputeAnimations(WindowData windowData)
    {
      if (Metronome == null)
        throw new InvalidOperationException("Metronome must be set before computing animations.");

      windowData.PreComputeAnimation(Metronome);

      if (windowData.Unresponsive)
        windowData.ComputeAnimationWhenUnresponsive(Metronome);
    }

    /// <summary>
    /// Computes animation data for all windows.
    /// </summary>
    public void ComputeAllAnimations()
    {
      if (Metronome == null)
        throw new InvalidOperationException("Metronome must be set before computing animations.");

      foreach (var window in WindowCollection)
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

    /// <summary>
    /// Adds a window to the collection and maintains sort order.
    /// </summary>
    public void AddWindow(WindowData windowData)
    {
      var idx = FindAddIndex(WindowCollection, windowData);
      WindowCollection.Insert(idx, windowData);
      SubscribeChangeEvent(windowData);

      RequestRecompute();
      NotifyChanged();
    }

    public void AddWindows(IEnumerable<WindowData> windows)
    {
      if (!windows.Any()) return;

      BeginUpdate();
      foreach (var window in windows) AddWindow(window);
      EndUpdate();
    }

    /// <summary>
    /// Removes a window by its unique identifier.
    /// </summary>
    public bool RemoveWindow(string id)
    {
      if (string.IsNullOrEmpty(id)) return false;

      var windowData = WindowCollection.FirstOrDefault(w => w.ID == id);
      if (windowData == default) return false;

      UnsubscribeChangeEvent(windowData);
      WindowCollection.Remove(windowData);

      RequestRecompute();
      NotifyChanged();

      return true;
    }

    public int RemoveWindows(IEnumerable<string> ids)
    {
      if (!ids.Any()) return 0;

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
      if (string.IsNullOrEmpty(id)) return null;

      var result = WindowCollection.FirstOrDefault(w => w.ID == id);

      if (result == default) return null;

      return result;
    }

    public IReadOnlyList<WindowData> GetWindows(IEnumerable<string> ids)
    {
      var result = new List<WindowData>();
      foreach (var id in ids)
      {
        var window = GetWindow(id);
        if (window != null) result.Add(window);
      }
      return result;
    }

    public IReadOnlyList<WindowData> GetAllWindows() => WindowCollection;

    /// <summary>
    /// Returns all windows sorted by layer for correct render order.
    /// </summary>
    public IReadOnlyList<WindowData> GetWindowsByLayer()
    {
      var windows = new List<WindowData>(WindowCollection);
      windows.Sort((a, b) => a.Layer.CompareTo(b.Layer));
      return windows;
    }

    /// <summary>
    /// Finds the insertion index for a window to keep the list sorted by StartBeat.
    /// </summary>
    public int FindAddIndex(List<WindowData> list, WindowData target)
    {
      if (list.Count == 0) return 0;

      int left = 0, right = list.Count - 1;
      while (left <= right)
      {
        int mid = left + (right - left) / 2;
        if (list[mid].StartBeat <= target.StartBeat) left = mid + 1;
        else right = mid - 1;
      }
      return left;
    }
  }
}
