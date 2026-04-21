using System;
using System.Collections.Generic;
using System.Linq;
using Winithm.Core.Common;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// Tracks the index of the last active event to speed up progressive timeline iterations.
  /// </summary>
  public class Cursor
  {
    internal int LastIndex;
    public Cursor() { LastIndex = 0; }
    public void Reset() { LastIndex = 0; }
  }

  /// <summary>
  /// Tracks timeline events for generic properties, parsing and computing values at exact points.
  /// </summary>
  public class StoryboardManager<TProp> : IDeepCloneable<StoryboardManager<TProp>>
  {
    public event Action<StoryboardManager<TProp>> OnStoryboardChanged;

    private int _updateLockCount = 0;

    /// <summary>
    /// Suspends event notifications to allow multiple property edits without triggering updates overhead.
    /// </summary>
    public void BeginUpdate() => _updateLockCount++;

    /// <summary>
    /// Resumes event notifications and recalculates state if edits were made.
    /// </summary>
    public void EndUpdate(bool success = true)
    {
      if (_updateLockCount > 0) _updateLockCount--;
      if (_updateLockCount == 0 && success) OnStoryboardChanged?.Invoke(this);
    }

    private void NotifyChanged()
    {
      if (_updateLockCount == 0) OnStoryboardChanged?.Invoke(this);
    }

    public Dictionary<TProp, List<EventData>> EventCollection { get; private set; } = new Dictionary<TProp, List<EventData>>();
    public Dictionary<TProp, Cursor> PropertyCursors { get; private set; } = new Dictionary<TProp, Cursor>();

    private readonly Dictionary<EventData, TProp> _eventKeyMap = new Dictionary<EventData, TProp>();

    public StoryboardManager<TProp> DeepClone(BeatTime? offset)
    {
      var newStoryboard = new StoryboardManager<TProp>();

      newStoryboard.BeginUpdate();

      foreach (var events in EventCollection)
      {
        foreach (var evt in events.Value)
          newStoryboard.AddEvent(events.Key, evt.DeepClone(offset));
      }

      newStoryboard.EndUpdate();

      return newStoryboard;
    }

    public static StoryboardProperty ParseEventProperty(string prop)
    {
      switch (prop)
      {
        case "Move_X": return StoryboardProperty.X;
        case "Move_Y": return StoryboardProperty.Y;
        case "Scale": return StoryboardProperty.Scale;
        case "Scale_X": return StoryboardProperty.ScaleX;
        case "Scale_Y": return StoryboardProperty.ScaleY;
        case "Rotation": return StoryboardProperty.Rotation;
        case "Color_R": return StoryboardProperty.ColorR;
        case "Color_G": return StoryboardProperty.ColorG;
        case "Color_B": return StoryboardProperty.ColorB;
        case "Color_A": return StoryboardProperty.ColorA;
        case "Note_A": return StoryboardProperty.NoteA;
        case "Title": return StoryboardProperty.Title;
        case "Speed": return StoryboardProperty.Speed;
        default: return StoryboardProperty.Custom;
      }
    }

    public static string FormatEventProperty(StoryboardProperty type, string customProperty)
    {
      if (type == StoryboardProperty.Custom) return customProperty;
      switch (type)
      {
        case StoryboardProperty.X: return "Move_X";
        case StoryboardProperty.Y: return "Move_Y";
        case StoryboardProperty.Scale: return "Scale";
        case StoryboardProperty.ScaleX: return "Scale_X";
        case StoryboardProperty.ScaleY: return "Scale_Y";
        case StoryboardProperty.Rotation: return "Rotation";
        case StoryboardProperty.ColorR: return "Color_R";
        case StoryboardProperty.ColorG: return "Color_G";
        case StoryboardProperty.ColorB: return "Color_B";
        case StoryboardProperty.ColorA: return "Color_A";
        case StoryboardProperty.NoteA: return "Note_A";
        case StoryboardProperty.Title: return "Title";
        case StoryboardProperty.Speed: return "Speed";
        default: return "";
      }
    }

    public static EventData ParseEventLine(string trimmed, out StoryboardProperty type, out string rawPropertyName)
    {
      var evt = new EventData();
      type = StoryboardProperty.Custom;
      rawPropertyName = "";

      var parts = new List<string>();
      bool inQuotes = false;
      int tokenStart = 2;

      for (int i = 2; i < trimmed.Length; i++)
      {
        char c = trimmed[i];
        if (c == '\"') inQuotes = !inQuotes;
        else if (c == ' ' && !inQuotes)
        {
          if (i > tokenStart) parts.Add(trimmed.Substring(tokenStart, i - tokenStart).Trim('\"'));
          tokenStart = i + 1;
        }
      }
      if (tokenStart < trimmed.Length)
      {
        string finalPart = trimmed.Substring(tokenStart).Trim();
        if (finalPart.Length > 0) parts.Add(finalPart.Trim('\"'));
      }

      if (parts.Count >= 1) evt.ID = parts[0];
      if (parts.Count >= 2)
      {
        rawPropertyName = parts[1];
        type = ParseEventProperty(rawPropertyName);
      }
      if (parts.Count >= 3) evt.StartBeat = BeatTime.Parse(parts[2]);
      if (parts.Count >= 4) evt.Length = ParserUtils.TryParseDouble(parts[3], out double length) ? length : 0;
      if (parts.Count >= 5) evt.From = AnyValue.Parse(parts[4]);
      if (parts.Count >= 6) evt.To = AnyValue.Parse(parts[5]);
      if (parts.Count >= 7)
      {
        if (parts[6].Contains("|"))
        {
          evt.Easing = EasingType.Bezier;
          evt.EasingBezier = AnyValue.Parse(parts[6]);
        }
        else
        {
          evt.Easing = EasingFunctions.ParseEasing(parts[6]);
        }
      }
      return evt;
    }

    public static string GenerateEventLine(EventData evt, StoryboardProperty type, string customProperty = null, int indent = 2)
    {
      string easingStr = evt.Easing == EasingType.Bezier ? evt.EasingBezier.ToString() : evt.Easing.ToString();
      string propStr = FormatEventProperty(type, customProperty);
      string result = $"/ {evt.ID} {propStr} {evt.StartBeat} {evt.Length} {evt.From} {evt.To} {easingStr}";
      return result.PadLeft(indent);
    }

    private void SubscribeChangeEvent(TProp prop, EventData evt)
    {
      evt.OnStartBeatChanged -= HandleStartBeatChanged;
      evt.OnStartBeatChanged += HandleStartBeatChanged;
      evt.OnDataChanged -= HandleDataChanged;
      evt.OnDataChanged += HandleDataChanged;
      _eventKeyMap[evt] = prop;
    }

    private void UnSubscribeChangeEvent(EventData evt)
    {
      evt.OnStartBeatChanged -= HandleStartBeatChanged;
      evt.OnDataChanged -= HandleDataChanged;
      _eventKeyMap.Remove(evt);
    }

    private void HandleStartBeatChanged(EventData evt)
    {
      if (_eventKeyMap.TryGetValue(evt, out var key)) OnEventStartBeatChanged(key, evt);
    }

    private void HandleDataChanged(EventData evt)
    {
      if (_eventKeyMap.ContainsKey(evt)) NotifyChanged();
    }

    public int AddEvent(TProp prop, EventData evt)
    {
      if (!EventCollection.TryGetValue(prop, out var list))
      {
        list = new List<EventData>();
        EventCollection[prop] = list;
        PropertyCursors[prop] = new Cursor();
      }

      int index = FindAddIndex(list, evt);
      list.Insert(index, evt);
      PropertyCursors[prop].Reset();

      SubscribeChangeEvent(prop, evt);
      NotifyChanged();

      return index;
    }

    public int[] AddEvents(TProp prop, List<EventData> evts)
    {
      if (evts.Count == 0) return Array.Empty<int>();

      BeginUpdate();

      int[] indices = new int[evts.Count];
      for (int i = 0; i < evts.Count; i++)
        indices[i] = AddEvent(prop, evts[i]);

      EndUpdate();

      return indices;
    }

    public bool RemoveEvent(TProp prop, EventData evt)
    {
      if (!EventCollection.TryGetValue(prop, out var list)) return false;
      if (!list.Remove(evt)) return false;

      UnSubscribeChangeEvent(evt);

      if (list.Count == 0)
      {
        EventCollection.Remove(prop);
        PropertyCursors.Remove(prop);
      }
      else PropertyCursors[prop].Reset();

      NotifyChanged();
      return true;
    }

    public int RemoveEvents(TProp prop, List<EventData> evts)
    {
      if (evts.Count == 0) return 0;
      
      BeginUpdate();

      int success = evts.Count(evt => RemoveEvent(prop, evt));

      EndUpdate(success > 0);
      
      return success;
    }

    public bool RemoveEvent(EventData evt)
    {
      if (_eventKeyMap.TryGetValue(evt, out var prop))
      {
        return RemoveEvent(prop, evt);
      }
      return false;
    }

    public int RemoveEvents(List<EventData> evts)
    {
      if (evts.Count == 0) return 0;
      
      BeginUpdate();

      int success = evts.Count(evt => RemoveEvent(evt));

      EndUpdate(success > 0);
      
      return success;
    }

    public bool RemoveEvent(TProp prop, string id)
    {
      if (!EventCollection.TryGetValue(prop, out var list)) return false;

      var toRemove = list.FindAll(x => x.ID == id);
      if (toRemove.Count == 0) return false;

      foreach (var evt in toRemove) UnSubscribeChangeEvent(evt);
      list.RemoveAll(x => x.ID == id);

      if (list.Count == 0)
      {
        EventCollection.Remove(prop);
        PropertyCursors.Remove(prop);
      }
      else PropertyCursors[prop].Reset();

      NotifyChanged();
      return true;
    }

    public int RemoveEvents(TProp prop, List<string> ids)
    {
      if (ids.Count == 0) return 0;

      BeginUpdate();

      int success = ids.Count(id => RemoveEvent(prop, id));

      EndUpdate(success > 0);

      return success;
    }

    public bool RemoveEvent(string id)
    {
      if (EventCollection.Count == 0) return false;

      BeginUpdate();

      bool anySuccess = false;
      foreach (var key in EventCollection.Keys.ToList())
      {
        if (RemoveEvent(key, id)) anySuccess = true;
      }

      EndUpdate(anySuccess);
      
      return anySuccess;
    }

    public int RemoveEvents(List<string> ids)
    {
      if (ids.Count == 0) return 0;
      
      BeginUpdate();

      int success = ids.Count(id => RemoveEvent(id));

      EndUpdate(success > 0);
      
      return success;
    }

    public EventData GetEvent(TProp prop, EventData evt)
    {
      if (EventCollection.TryGetValue(prop, out var list) && list.Contains(evt)) return evt;
      throw new KeyNotFoundException($"Event {evt.ID} not found.");
    }

    public List<EventData> GetEvents(TProp prop, List<EventData> evts)
    {
      var result = new List<EventData>();
      if (EventCollection.TryGetValue(prop, out var list))
      {
        foreach (var evt in evts)
          if (list.Contains(evt)) result.Add(evt);
      }
      return result;
    }

    public (TProp prop, EventData evt) GetEvent(EventData evt)
    {
      if (_eventKeyMap.TryGetValue(evt, out var prop)) return (prop, evt);
      throw new KeyNotFoundException($"Event {evt.ID} not found.");
    }

    public Dictionary<TProp, EventData> GetEvents(List<EventData> evts)
    {
      var result = new Dictionary<TProp, EventData>();
      foreach (var evt in evts)
      {
        if (_eventKeyMap.TryGetValue(evt, out var prop)) result[prop] = evt;
      }
      return result;
    }

    public List<EventData> GetEvent(TProp prop, string id)
    {
      if (EventCollection.TryGetValue(prop, out var evts))
        return evts.Where(e => e.ID == id).ToList();
      throw new KeyNotFoundException($"Event {id} not found.");
    }

    public List<EventData> GetEvents(TProp prop, List<string> ids)
    {
      var result = new List<EventData>();
      if (EventCollection.TryGetValue(prop, out var list))
      {
        var idSet = new HashSet<string>(ids);
        result.AddRange(list.Where(e => idSet.Contains(e.ID)));
      }
      return result;
    }

    public (TProp prop, List<EventData> evts) GetEvent(string id)
    {
      foreach (var pair in EventCollection)
      {
        var found = pair.Value.Where(e => e.ID == id).ToList();
        if (found.Count > 0) return (pair.Key, found);
      }
      throw new KeyNotFoundException($"Event {id} not found.");
    }

    public Dictionary<TProp, List<EventData>> GetEvents(List<string> ids)
    {
      var result = new Dictionary<TProp, List<EventData>>();
      var idSet = new HashSet<string>(ids);

      foreach (var pair in EventCollection)
      {
        var found = pair.Value.Where(e => idSet.Contains(e.ID)).ToList();
        if (found.Count > 0)
        {
          if (!result.TryGetValue(pair.Key, out var evts))
            result[pair.Key] = new List<EventData>();
          result[pair.Key].AddRange(found);
        }
      }
      return result;
    }

    public List<EventData> GetPropEvents(TProp prop)
    {
      if (EventCollection.TryGetValue(prop, out var events)) return events;
      throw new KeyNotFoundException($"Property {prop} not found.");
    }

    public Dictionary<TProp, List<EventData>> GetAllEvents() => EventCollection;

    private void OnEventStartBeatChanged(TProp prop, EventData evt)
    {
      if (!EventCollection.TryGetValue(prop, out var list)) return;
      if (!list.Contains(evt)) return;

      list.Remove(evt);
      int index = FindAddIndex(list, evt);
      list.Insert(index, evt);

      PropertyCursors[prop].Reset();
      NotifyChanged();
    }

    public void SortAllEvents()
    {
      if (EventCollection.Count == 0) return;
      foreach (TProp prop in EventCollection.Keys.ToList()) SortPropEvents(prop);
      NotifyChanged();
    }

    public void SortPropEvents(TProp key)
    {
      if (!EventCollection.TryGetValue(key, out var events) || events.Count <= 1) return;
      
      events.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
      PropertyCursors[key].Reset();
      NotifyChanged();
    }

    /// <summary>Finds insertion index to maintain sorted stability.</summary>
    public int FindAddIndex(List<EventData> list, EventData evt)
    {
      if (list.Count == 0) return 0;
      int left = 0, right = list.Count - 1;
      while (left <= right)
      {
        int mid = left + (right - left) / 2;
        if (list[mid].StartBeat <= evt.StartBeat) left = mid + 1;
        else right = mid - 1;
      }
      return left;
    }

    public AnyValue Evaluate(TProp prop, double currentBeat, AnyValue defaultValue, bool isScrubbing = false)
    {
      if (!EventCollection.TryGetValue(prop, out var events) || events.Count == 0)
        return defaultValue;

      int idx = AdvanceCursor(prop, currentBeat, isScrubbing);
      return EvaluateRecursive(events, idx, currentBeat, defaultValue);
    }

    private AnyValue EvaluateRecursive(List<EventData> events, int idx, double currentBeat, AnyValue defaultValue)
    {
      if (idx < 0) return defaultValue;

      var evt = events[idx];
      var resolvedFrom = evt.From.Type == AnyValueType.Inherited
        ? EvaluateRecursive(events, idx - 1, currentBeat, defaultValue)
        : evt.From;

      return Interpolate(evt, currentBeat, resolvedFrom);
    }

    private AnyValue Interpolate(EventData evt, double currentBeat, AnyValue resolvedFrom)
    {
      double startBeat = evt.StartBeat.AbsoluteValue;
      double endBeat = startBeat + evt.Length;

      if (currentBeat >= endBeat) return evt.To;

      double length = evt.Length;
      double t = length > 0.0 ? (currentBeat - startBeat) / length : 1.0;

      t = evt.Easing == EasingType.Bezier
        ? EasingFunctions.EvaluateBezier(evt.EasingBezier, t)
        : EasingFunctions.Evaluate(evt.Easing, t);

      return AnyValue.Lerp(resolvedFrom, evt.To, t);
    }

    private int AdvanceCursor(TProp prop, double currentBeat, bool isScrubbing)
    {
      var events = EventCollection[prop];
      var cursor = PropertyCursors[prop];

      int n = events.Count;
      int last = cursor.LastIndex;
      if (last >= n) last = n - 1;

      if (isScrubbing)
      {
        int idx = FindLastStarted(prop, currentBeat);
        cursor.LastIndex = Math.Max(0, idx);
        return idx;
      }

      // If we've moved backward in time before our current cursor event starts
      if (events[last].StartBeat.AbsoluteValue > currentBeat)
      {
        // Skip binary search if before the very first element
        if (last == 0) return -1;

        int idx = FindLastStarted(prop, currentBeat);
        cursor.LastIndex = Math.Max(0, idx);
        return idx;
      }

      // Fast forward timeline continuously
      while (last + 1 < n && events[last + 1].StartBeat.AbsoluteValue <= currentBeat)
      {
        last++;
      }

      cursor.LastIndex = last;
      return last;
    }

    private int FindLastStarted(TProp prop, double currentBeat)
    {
      var events = EventCollection[prop];
      int left = 0, right = events.Count - 1, best = -1;

      while (left <= right)
      {
        int mid = left + (right - left) / 2;
        if (events[mid].StartBeat.AbsoluteValue <= currentBeat)
        {
          best = mid;
          left = mid + 1;
        }
        else right = mid - 1;
      }
      return best;
    }
  }
}