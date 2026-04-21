using Godot;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Logic;
using Winithm.Core.Common;

namespace Winithm.Core.Controllers
{
  [Tool]
  public class ThemeChannelController : Node
  {
    private Dictionary<string, ThemeChannelData> _themeChannels = new Dictionary<string, ThemeChannelData>();
    private Dictionary<string, (float LastBeat, Color Color, float NoteAlpha)> _lastStates = new Dictionary<string, (float, Color, float)>();

    private Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>> _cursors
      = new Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>>();

    public void LoadThemeChannels(List<ThemeChannelData> channels)
    {
      _themeChannels.Clear();
      _lastStates.Clear();
      _cursors.Clear();

      if (channels == null) return;

      foreach (var tc in channels)
      {
        _themeChannels[tc.ID] = tc;
        _lastStates[tc.ID] = (-1f, new Color(tc.InitR, tc.InitG, tc.InitB, tc.InitA), tc.InitNoteA);

        var propCursors = new Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>();
        if (tc.StoryboardEvents != null)
        {
          foreach (var prop in tc.StoryboardEvents.Keys)
            propCursors[prop] = new StoryboardEvaluator.Cursor();
        }
        _cursors[tc.ID] = propCursors;
      }
    }

    public bool HasThemeChannel(string id) => _themeChannels.ContainsKey(id);

    public (Color WindowColor, float NoteA) GetThemeColor(string id, float currentBeat, Color fallback)
    {
      if (string.IsNullOrEmpty(id) || !_themeChannels.ContainsKey(id)) return (fallback, 1f);

      var stateVal = _lastStates[id];
      if (Mathf.Abs(stateVal.LastBeat - currentBeat) <= 0.0001f)
        return (stateVal.Color, stateVal.NoteAlpha);

      return ForceGetThemeColor(id, currentBeat, fallback);
    }

    public (Color WindowColor, float NoteA) ForceGetThemeColor(string id, float currentBeat, Color fallback)
    {
      if (string.IsNullOrEmpty(id) || !_themeChannels.TryGetValue(id, out var tc)) return (fallback, 1f);

      var cursors = _cursors[id];
      float r = EvaluateProperty(tc, StoryboardProperty.ColorR, currentBeat, tc.InitR, GetCursor(cursors, StoryboardProperty.ColorR));
      float g = EvaluateProperty(tc, StoryboardProperty.ColorG, currentBeat, tc.InitG, GetCursor(cursors, StoryboardProperty.ColorG));
      float b = EvaluateProperty(tc, StoryboardProperty.ColorB, currentBeat, tc.InitB, GetCursor(cursors, StoryboardProperty.ColorB));
      float a = EvaluateProperty(tc, StoryboardProperty.ColorA, currentBeat, tc.InitA, GetCursor(cursors, StoryboardProperty.ColorA));
      float noteA = EvaluateProperty(tc, StoryboardProperty.NoteA, currentBeat, tc.InitNoteA, GetCursor(cursors, StoryboardProperty.NoteA));

      Color color = new Color(r, g, b, a);
      _lastStates[id] = (currentBeat, color, noteA);
      return (color, noteA);
    }

    private StoryboardEvaluator.Cursor GetCursor(Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor> cursors, StoryboardProperty prop)
    {
      if (cursors.TryGetValue(prop, out var cursor)) return cursor;
      return null;
    }

    private float EvaluateProperty(ThemeChannelData tc, StoryboardProperty propType, float beat, float defaultValue, StoryboardEvaluator.Cursor cursor)
    {
      if (tc.StoryboardEvents == null || !tc.StoryboardEvents.TryGetValue(propType, out var events)) return defaultValue;
      return StoryboardEvaluator.Evaluate(events, beat, new AnyValue(defaultValue), cursor).X;
    }
  }
}
