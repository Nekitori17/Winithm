using Godot;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Common;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  [Tool]
  public class ThemeChannelController : Node
  {
    private ThemeChannelManager _themeManager;
    private readonly Dictionary<string, (float LastBeat, Color Color, float NoteAlpha)> _lastStates = new Dictionary<string, (float, Color, float)>();

    public void LoadThemeChannels(ThemeChannelManager manager)
    {
      _themeManager = manager ?? new ThemeChannelManager();

      foreach (var tc in _themeManager.ThemeChannelCollection.Values)
      {
        _lastStates[tc.ID] = (-1f, new Color(tc.InitR, tc.InitG, tc.InitB, tc.InitA), tc.InitNoteA);
      }
    }

    public bool HasThemeChannel(string id) => _themeManager.ContainsThemeChannel(id);

    public (Color WindowColor, float NoteA)? GetThemeColor(
      string id, float currentBeat
    )
    {
      if (string.IsNullOrEmpty(id) || !_themeManager.ContainsThemeChannel(id)) 
        return null;

      var stateVal = _lastStates[id];
      if (Mathf.Abs(stateVal.LastBeat - currentBeat) <= 0.0001f)
        return (stateVal.Color, stateVal.NoteAlpha);

      return ForceGetThemeColor(id, currentBeat, false);
    }

    public (Color WindowColor, float NoteA)? ForceGetThemeColor(
      string id, float currentBeat, bool _force = true
    )
    {
      if (string.IsNullOrEmpty(id) || !_themeManager.ContainsThemeChannel(id)) 
        return null;
      
      var tc = _themeManager.GetThemeChannel(id);

      float r = EvaluateProperty(tc, StoryboardProperty.ColorR, currentBeat, tc.InitR, _force);
      float g = EvaluateProperty(tc, StoryboardProperty.ColorG, currentBeat, tc.InitG, _force);
      float b = EvaluateProperty(tc, StoryboardProperty.ColorB, currentBeat, tc.InitB, _force);
      float a = EvaluateProperty(tc, StoryboardProperty.ColorA, currentBeat, tc.InitA, _force);
      float noteA = EvaluateProperty(tc, StoryboardProperty.NoteA, currentBeat, tc.InitNoteA, _force);

      Color color = new Color(r, g, b, a);
      _lastStates[id] = (currentBeat, color, noteA);

      return (color, noteA);
    }

    private float EvaluateProperty(
      ThemeChannelData tc, StoryboardProperty prop, double beat, float defaultValue, bool _force = true
    )
    {
      if (tc.StoryboardEvents == null 
        || !tc.StoryboardEvents.EventCollection.TryGetValue(prop, out var events)
      ) return defaultValue;

      return tc.StoryboardEvents.Evaluate(prop, beat, new AnyValue(defaultValue), _force).X;
    }
  }
}
