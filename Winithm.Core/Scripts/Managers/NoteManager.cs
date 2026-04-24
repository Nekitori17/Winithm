using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Managers
{
  public enum NoteSide
  {
    Top,
    Bottom,
    Left,
    Right
  }

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

    /// <summary>Sorted beats where combo increments occur (Hold → end beat, others → start beat).</summary>
    public double[] ComboEventBeats { get; private set; } = Array.Empty<double>();
    /// <summary>Prefix-sum of combo values aligned with ComboEventBeats.</summary>
    public int[] ComboPrefixSum { get; private set; } = Array.Empty<int>();

    public BeatTime ExpectedStartFocusBeat { get; private set; } = BeatTime.Max;
    public BeatTime ExpectedEndCloseBeat { get; private set; } = BeatTime.Max;
    public int TotalComboCount { get; private set; } = 0;

    private int _updateLockCount = 0;
    private bool _needsRecompute = false;

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
    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success)
      {
        CommitRecompute();
        OnUpdated?.Invoke(this);
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

    private void NotifyChanged()
    {
      if (_updateLockCount == 0)
      {
        CommitRecompute();
        OnUpdated?.Invoke(this);
      }
    }

    public void SetWindowData(WindowData windowData)
    {
      WindowData = windowData;
      RequestRecompute();
      NotifyChanged();
    }

    public static NoteData ParseNoteLine(string text, out NoteSide side)
    {
      side = NoteSide.Bottom;

      string[] parts = text.Trim().Substring(2).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

      var current = new NoteData();

      if (parts.Length >= 1)
        current.ID = parts[0];
      if (parts.Length >= 2)
        current.Type = Enum.TryParse<NoteType>(parts[1], true, out var t) ? t : NoteType.Tap;
      if (parts.Length >= 3)
        current.StartBeat = BeatTime.TryParse(parts[2], out var sb) ? sb : BeatTime.Zero;
      if (parts.Length >= 4)
        current.Length = double.TryParse(parts[3], out var l) ? l : 0;
      if (parts.Length >= 5)
        current.X = float.TryParse(parts[4], out var x) ? x : 0.5f;
      if (parts.Length >= 6)
        current.Width = float.TryParse(parts[5], out var w) ? w : 0.5f;
      if (parts.Length >= 7)
        side = Enum.TryParse<NoteSide>(parts[6], true, out var s) ? s : NoteSide.Bottom;
      if (parts.Length >= 8)
        current.FakeType = int.TryParse(parts[7], out var ft) ? ft : 0;

      return current;
    }

    public static string GenerateNoteLine(NoteSide side, NoteData data, int indent = 2)
    {
      string result = $"# {data.ID} {data.Type} {data.StartBeat} {data.Length} {data.X} {data.Width} {side} {data.FakeType}";
      return result.PadLeft(indent);
    }

    /// <summary>
    /// Re-evaluates note boundaries, MaxEndBeats, and combo prefix-sum.
    /// </summary>
    public void Compute()
    {
      if (WindowData == null)
      {
        ExpectedStartFocusBeat = BeatTime.Max;
        ExpectedEndCloseBeat = BeatTime.Max;
        TotalComboCount = 0;
        MaxEndBeats.Clear();
        ComboEventBeats = Array.Empty<double>();
        ComboPrefixSum = Array.Empty<int>();
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

      TotalComboCount = 0;
      MaxEndBeats.Clear();

      // Collect combo events and build MaxEndBeats in the same pass
      var comboEvents = new List<(double beat, int combo)>();

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
            if (note.Type == NoteType.Hold)
            {
              // Hold: 2 combo scored at END beat
              comboEvents.Add((note.StartBeat.AbsoluteValue + note.Length, 2));
              TotalComboCount += 2;
            }
            else
            {
              comboEvents.Add((note.StartBeat.AbsoluteValue, 1));
              TotalComboCount++;
            }
          }

          runningMax = Math.Max(runningMax, note.StartBeat.AbsoluteValue + note.Length);
          maxEnds[i] = runningMax;
        }
        MaxEndBeats[sideNotes.Key] = maxEnds;
      }

      // Sort combo events by beat and build prefix-sum
      comboEvents.Sort((a, b) => a.beat.CompareTo(b.beat));
      ComboEventBeats = new double[comboEvents.Count];
      ComboPrefixSum = new int[comboEvents.Count];
      int runningCombo = 0;
      for (int i = 0; i < comboEvents.Count; i++)
      {
        runningCombo += comboEvents[i].combo;
        ComboEventBeats[i] = comboEvents[i].beat;
        ComboPrefixSum[i] = runningCombo;
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

      note.OnInvalidate -= HandleInvalidate;
      note.OnInvalidate += HandleInvalidate;

      note.OnUpdated -= HandleUpdated;
      note.OnUpdated += HandleUpdated;

      _eventKeyMap[note] = side;
    }

    private void UnsubscribeChangeEvent(NoteData note)
    {
      note.OnStartBeatChanged -= HandleStartBeatChanged;
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

      RequestRecompute();
      NotifyChanged();
    }

    private void HandleInvalidate(NoteData note) { RequestRecompute(); NotifyChanged(); }
    private void HandleUpdated(NoteData note) => NotifyChanged();

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
      RequestRecompute();
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
      RequestRecompute();
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
      RequestRecompute();
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

    public IReadOnlyList<NoteData> GetNote(NoteSide side, string id)
    {
      if (NoteCollection.TryGetValue(side, out var notes))
        return notes.FindAll(n => n.ID == id);
      throw new KeyNotFoundException($"Note {id} not found.");
    }

    public IReadOnlyList<NoteData> GetNotes(NoteSide side, IEnumerable<string> ids)
    {
      if (!ids.Any() || NoteCollection.Count == 0) return Array.Empty<NoteData>();

      var result = new List<NoteData>();
      foreach (var id in ids)
        try { result.AddRange(GetNote(side, id)); }
        catch (KeyNotFoundException) { continue; }
      return result;
    }

    public IReadOnlyList<NoteData> GetNote(string id)
    {
      var result = new List<NoteData>();
      foreach (var pair in NoteCollection)
      {
        result.AddRange(pair.Value.FindAll(n => n.ID == id));
      }

      if (result.Count == 0) throw new KeyNotFoundException($"Note {id} not found.");
      return result;
    }

    public IReadOnlyList<NoteData> GetNotes(IEnumerable<string> ids)
    {
      var result = new List<NoteData>();
      foreach (var id in ids)
        try { result.AddRange(GetNote(id)); }
        catch (KeyNotFoundException) { continue; }
      return result;
    }

    public IReadOnlyList<NoteData> GetSideNotes(NoteSide side)
    {
      if (NoteCollection.TryGetValue(side, out var notes)) return notes;

      NoteCollection[side] = new List<NoteData>();
      return NoteCollection[side];
    }

    public IReadOnlyDictionary<NoteSide, List<NoteData>> GetAllNotes() => NoteCollection;

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
