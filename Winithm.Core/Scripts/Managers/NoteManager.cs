using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Manages Note segments, boundaries, spatial extents, and rendering lifetimes.
  /// </summary>
  public class NoteManager : IDeepCloneable<NoteManager>
  {
    public event Action<NoteManager> OnLifeCycleChanged;
    public event Action<NoteManager> OnUpdated;

    public WindowData WindowData { get; private set; }

    public Dictionary<NoteSide, List<NoteData>> NoteCollection { get; private set; } = new Dictionary<NoteSide, List<NoteData>>();
    public Dictionary<NoteSide, double[]> MaxEndBeats { get; private set; } = new Dictionary<NoteSide, double[]>();

    public BeatTime ExpectedStartFocusBeat { get; private set; } = BeatTime.Max;
    public BeatTime ExpectedEndCloseBeat { get; private set; } = BeatTime.Max;
    public int TotalHittableNoteCount { get; private set; } = 0;

    private int _updateLockCount = 0;

    public NoteManager DeepClone(ObjectFactory objectFactory, BeatTime? offset)
    {
      var cloned = new NoteManager();
      cloned.SetWindowData(this.WindowData);

      cloned.BeginUpdate();
      foreach (var sideNotes in NoteCollection)
      {
        foreach (var note in sideNotes.Value)
          cloned.AddNote(sideNotes.Key, note.DeepClone(objectFactory, offset));
      }
      cloned.EndUpdate();

      return cloned;
    }

    /// <summary>
    /// Suspends compute calculations to allow multiple edits without triggering massive overhead.
    /// </summary>
    public void BeginUpdate() => _updateLockCount++;

    /// <summary>
    /// Resumes notifications and runs Compute() once if edits were made.
    /// </summary>
    public void EndUpdate(bool success = true, bool needReCompute = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success)
      {
        if (needReCompute) Compute();
        OnUpdated?.Invoke(this);
      }
    }

    private void NotifyChanged(bool needReCompute = true)
    {
      if (_updateLockCount == 0)
      {
        if (needReCompute) Compute();
        OnUpdated?.Invoke(this);
      }
    }

    public void SetWindowData(WindowData windowData)
    {
      WindowData = windowData;
      NotifyChanged();
    }

    public static NoteType ParseNoteType(string text)
    {
      switch (text.Trim())
      {
        case "Tap": return NoteType.Tap;
        case "Hold": return NoteType.Hold;
        case "Drag": return NoteType.Drag;
        case "Focus": return NoteType.Focus;
        case "Close": return NoteType.Close;
        default:
          GD.PushError($"[WinithmParser] Unknown note type: '{text}', defaulting to Tap.");
          return NoteType.Tap;
      }
    }

    public static NoteSide ParseSide(string text)
    {
      switch (text.Trim())
      {
        case "Top": return NoteSide.Top;
        case "Bottom": return NoteSide.Bottom;
        case "Left": return NoteSide.Left;
        case "Right": return NoteSide.Right;
        default:
          GD.PushError($"[WinithmParser] Unknown side: '{text}', defaulting to Bottom.");
          return NoteSide.Bottom;
      }
    }

    /// <summary>
    /// Re-evaluates note boundaries and tracks the trailing tails of hold notes.
    /// </summary>
    public void Compute()
    {
      if (WindowData == null)
      {
        ExpectedStartFocusBeat = BeatTime.Max;
        ExpectedEndCloseBeat = BeatTime.Max;
        TotalHittableNoteCount = 0;
        MaxEndBeats.Clear();
        return;
      }

      BeatTime prevExpectedEndCloseBeat = ExpectedEndCloseBeat;
      BeatTime prevWindowEndBeat = WindowData.EndBeat;

      ExpectedStartFocusBeat = WindowData.UnFocus
        ? WindowData.StartBeat
        : BeatTime.Max;
      ExpectedEndCloseBeat = WindowData.EndBeat;

      foreach (var notes in NoteCollection.Values)
      {
        foreach (NoteData note in notes)
        {
          if (note.Type == NoteType.Focus && note.IsHittable)
          {
            if (note.StartBeat < ExpectedStartFocusBeat)
              ExpectedStartFocusBeat = note.StartBeat;
          }

          if (note.Type == NoteType.Close && note.IsHittable)
          {
            if (note.StartBeat < ExpectedEndCloseBeat)
              ExpectedEndCloseBeat = note.StartBeat;
            break;
          }
        }
      }

      WindowData.EndBeat = ExpectedEndCloseBeat;

      TotalHittableNoteCount = 0;
      MaxEndBeats.Clear();

      foreach (var sideNotes in NoteCollection)
      {
        var list = sideNotes.Value;
        double[] maxEnds = new double[list.Count];
        double runningMax = double.MinValue;

        for (int i = 0; i < list.Count; i++)
        {
          var note = list[i];
          if (note.StartBeat >= ExpectedStartFocusBeat
              && note.StartBeat <= ExpectedEndCloseBeat
              && note.IsHittable)
          {
            TotalHittableNoteCount++;
          }

          runningMax = Math.Max(runningMax, note.StartBeat.AbsoluteValue + note.Length);
          maxEnds[i] = runningMax;
        }
        MaxEndBeats[sideNotes.Key] = maxEnds;
      }

      if (
        prevExpectedEndCloseBeat != ExpectedEndCloseBeat ||
        prevWindowEndBeat != WindowData.EndBeat
      )
      {
        OnLifeCycleChanged?.Invoke(this);
      }
    }

    // ==========================================
    // Event Management
    // ==========================================

    private readonly Dictionary<NoteData, NoteSide> _eventKeyMap = new Dictionary<NoteData, NoteSide>();

    private void SubscribeChangeEvent(NoteSide side, NoteData note)
    {
      note.OnStartBeatChanged -= HandleStartBeatChanged;
      note.OnStartBeatChanged += HandleStartBeatChanged;

      note.OnSideChanged -= HandleSideChanged;
      note.OnSideChanged += HandleSideChanged;

      note.OnInvalidate -= HandleInvalidate;
      note.OnInvalidate += HandleInvalidate;

      note.OnUpdated -= HandleUpdated;
      note.OnUpdated += HandleUpdated;

      _eventKeyMap[note] = side;
    }

    private void UnsubscribeChangeEvent(NoteData note)
    {
      note.OnStartBeatChanged -= HandleStartBeatChanged;
      note.OnSideChanged -= HandleSideChanged;
      note.OnInvalidate -= HandleInvalidate;
      note.OnUpdated -= HandleUpdated;

      _eventKeyMap.Remove(note);
    }

    private void HandleStartBeatChanged(NoteData note)
    {
      if (!_eventKeyMap.TryGetValue(note, out var side)) return;

      var list = NoteCollection[side];
      list.Remove(note);
      int index = FindAddIndex(list, note);
      list.Insert(index, note);

      NotifyChanged();
    }

    private void HandleSideChanged(NoteData note)
    {
      if (!_eventKeyMap.TryGetValue(note, out var currentSide)) return;
      if (note.Side == currentSide) { NotifyChanged(); return; }

      BeginUpdate();
      RemoveNote(currentSide, note);
      AddNote(note.Side, note);
      EndUpdate();
    }

    private void HandleInvalidate(NoteData note) => NotifyChanged();
    private void HandleUpdated(NoteData note) => NotifyChanged(false);

    // ==========================================
    // Lifecycle Management
    // ==========================================

    public int AddNote(NoteSide side, NoteData note)
    {
      if (!NoteCollection.TryGetValue(side, out var list))
      {
        list = new List<NoteData>();
        NoteCollection[side] = list;
      }

      int index = FindAddIndex(list, note);
      list.Insert(index, note);

      SubscribeChangeEvent(side, note);
      NotifyChanged();

      return index;
    }

    public int[] AddNotes(NoteSide side, List<NoteData> notes)
    {
      if (notes.Count == 0) return Array.Empty<int>();

      BeginUpdate();

      int[] indices = new int[notes.Count];
      for (int i = 0; i < notes.Count; i++)
        indices[i] = AddNote(side, notes[i]);

      EndUpdate();
      return indices;
    }

    public bool RemoveNote(NoteSide side, NoteData note)
    {
      if (!NoteCollection.TryGetValue(side, out var list)) return false;
      if (!list.Remove(note)) return false;

      UnsubscribeChangeEvent(note);

      if (list.Count == 0) NoteCollection.Remove(side);
      NotifyChanged();

      return true;
    }

    public int RemoveNotes(NoteSide side, List<NoteData> notes)
    {
      if (notes.Count == 0) return 0;

      BeginUpdate();
      int success = notes.Count(n => RemoveNote(side, n));
      EndUpdate(success > 0);

      return success;
    }

    public bool RemoveNote(NoteData note)
    {
      if (_eventKeyMap.TryGetValue(note, out var side))
        return RemoveNote(side, note);
      return false;
    }

    public int RemoveNotes(List<NoteData> notes)
    {
      if (notes.Count == 0) return 0;

      BeginUpdate();
      int success = notes.Count(n => RemoveNote(n));
      EndUpdate(success > 0);

      return success;
    }

    public bool RemoveNote(NoteSide side, string id)
    {
      if (!NoteCollection.TryGetValue(side, out var list)) return false;

      var toRemove = list.FindAll(x => x.ID == id);
      if (toRemove.Count == 0) return false;

      foreach (var note in toRemove) UnsubscribeChangeEvent(note);
      list.RemoveAll(x => x.ID == id);

      if (list.Count == 0) NoteCollection.Remove(side);
      NotifyChanged();

      return true;
    }

    public int RemoveNotes(NoteSide side, List<string> ids)
    {
      if (ids.Count == 0) return 0;

      BeginUpdate();
      int success = ids.Count(id => RemoveNote(side, id));
      EndUpdate(success > 0);

      return success;
    }

    public bool RemoveNote(string id)
    {
      if (NoteCollection.Count == 0) return false;

      BeginUpdate();
      bool anySuccess = false;
      foreach (var side in NoteCollection.Keys.ToList())
      {
        if (RemoveNote(side, id)) anySuccess = true;
      }
      EndUpdate(anySuccess);

      return anySuccess;
    }

    public int RemoveNotes(List<string> ids)
    {
      if (ids.Count == 0) return 0;

      BeginUpdate();
      int success = ids.Count(id => RemoveNote(id));
      EndUpdate(success > 0);

      return success;
    }

    // ==========================================
    // Fetch Methods
    // ==========================================

    public NoteData GetNote(NoteSide side, NoteData note)
    {
      if (NoteCollection.TryGetValue(side, out var list) && list.Contains(note)) return note;
      throw new KeyNotFoundException($"Note {note.ID} not found.");
    }

    public List<NoteData> GetNote(string id)
    {
      var result = new List<NoteData>();
      foreach (var pair in NoteCollection)
      {
        result.AddRange(pair.Value.FindAll(n => n.ID == id));
      }

      if (result.Count == 0) throw new KeyNotFoundException($"Note {id} not found.");
      return result;
    }

    public List<NoteData> GetSideNotes(NoteSide side)
    {
      if (NoteCollection.TryGetValue(side, out var notes)) return notes;

      NoteCollection[side] = new List<NoteData>();
      return NoteCollection[side];
    }

    public Dictionary<NoteSide, List<NoteData>> GetAllNotes() => NoteCollection;

    public List<NoteData> GetNote(NoteSide side, string id)
    {
      if (NoteCollection.TryGetValue(side, out var notes))
        return notes.FindAll(n => n.ID == id);
      throw new KeyNotFoundException($"Note {id} not found.");
    }

    // ==========================================
    // Operations
    // ==========================================

    public int FindAddIndex(List<NoteData> list, NoteData target)
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

    public double[] GetMaxEndBeats(NoteSide side)
    {
      if (MaxEndBeats.TryGetValue(side, out var maxEnds)) return maxEnds;
      return Array.Empty<double>();
    }
  }
}
