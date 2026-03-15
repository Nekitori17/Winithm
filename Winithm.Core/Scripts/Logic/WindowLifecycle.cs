using System;
using System.Collections.Generic;
using Winithm.Core.Data;

namespace Winithm.Core.Logic
{
  /// <summary>
  /// Determines which windows are active at a given beat.
  /// Pre-sorts windows for O(log N) lookups instead of O(N).
  /// </summary>
  public class WindowLifecycle
  {
    private readonly List<WindowData> _sorted;
    private readonly List<WindowData> _activeBuffer = new List<WindowData>();
    private readonly List<NoteData> _noteBuffer = new List<NoteData>();

    /// <summary>
    /// Initializes the lifecycle manager with a pre-sorted copy of the window list.
    /// </summary>
    public WindowLifecycle(List<WindowData> windows)
    {
      _sorted = new List<WindowData>(windows);
      _sorted.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
    }

    /// <summary>
    /// Returns whether a window is active at the given beat.
    /// </summary>
    public static bool IsWindowActive(WindowData window, float currentBeat)
    {
      if (window == null || window.SpeedSteps.Count == 0) return false;
      return currentBeat >= window.StartBeat && currentBeat <= window.EndBeat;
    }

    /// <summary>
    /// Returns all active windows at the given beat.
    /// Reuses an internal buffer to avoid GC allocations.
    /// </summary>
    public List<WindowData> GetActiveWindows(float currentBeat)
    {
      _activeBuffer.Clear();

      // Binary search: find the first window that could possibly be active.
      // Any window with StartBeat > currentBeat cannot be active.
      int upperBound = FindUpperBound(currentBeat);

      // Only scan windows whose StartBeat <= currentBeat.
      for (int i = 0; i < upperBound; i++)
      {
        if (IsWindowActive(_sorted[i], currentBeat))
          _activeBuffer.Add(_sorted[i]);
      }

      return _activeBuffer;
    }

    /// <summary>
    /// Returns notes within the visible range for rendering.
    /// Reuses an internal buffer to avoid GC allocations.
    /// </summary>
    public List<NoteData> GetActiveNotes(WindowData window, float currentBeat, float lookAheadBeats)
    {
      _noteBuffer.Clear();
      if (window == null) return _noteBuffer;

      float rangeEnd = currentBeat + lookAheadBeats;

      for (int i = 0; i < window.Notes.Count; i++)
      {
        var note = window.Notes[i];
        if (note.EndBeat >= currentBeat && note.Start.AbsoluteValue <= rangeEnd)
          _noteBuffer.Add(note);
      }

      return _noteBuffer;
    }

    /// <summary>
    /// Binary search for the first index where StartBeat > beat.
    /// </summary>
    private int FindUpperBound(float beat)
    {
      int left = 0, right = _sorted.Count;

      while (left < right)
      {
        int mid = left + (right - left) / 2;
        if (_sorted[mid].StartBeat <= beat)
          left = mid + 1;
        else
          right = mid;
      }

      return left;
    }
  }
}
