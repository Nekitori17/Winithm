using Godot;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Logic;
using Winithm.Core.Common;

namespace Winithm.Core.Managers
{
  [Tool]
  public class GroupManager : Node
  {
    private Dictionary<string, GroupData> _groupDataMap = new Dictionary<string, GroupData>();
    private Dictionary<string, Node2D> _groupNodes = new Dictionary<string, Node2D>();
    private Dictionary<string, float> _lastUpdateBeat = new Dictionary<string, float>();

    private Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>> _cursors 
      = new Dictionary<string, Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>>();

    public void LoadGroups(List<GroupData> groups)
    {
      foreach (var node in _groupNodes.Values)
      {
        if (IsInstanceValid(node)) node.QueueFree();
      }
      _groupNodes.Clear();
      _groupDataMap.Clear();
      _lastUpdateBeat.Clear();
      _cursors.Clear();

      if (groups == null) return;

      foreach (var g in groups)
      {
        _groupDataMap[g.ID] = g;
        _lastUpdateBeat[g.ID] = -1f;

        var node = new Node2D
        {
          Name = string.IsNullOrEmpty(g.ID) ? "Group" : g.ID,
          Position = new Vector2(g.InitX, g.InitY),
          Scale = new Vector2(g.InitScaleX, g.InitScaleY),
          RotationDegrees = g.InitRotation
        };


        _groupNodes[g.ID] = node;

        var propCursors = new Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor>();
        if (g.StoryboardEvents != null)
        {
          foreach (var prop in g.StoryboardEvents.Keys)
            propCursors[prop] = new StoryboardEvaluator.Cursor();
        }
        _cursors[g.ID] = propCursors;
      }

      foreach (var g in groups)
      {
        var node = _groupNodes[g.ID];
        if (!string.IsNullOrEmpty(g.ParentGroupID) && _groupNodes.TryGetValue(g.ParentGroupID, out Node2D parentNode))
        {
          parentNode.AddChild(node);
        }
        else
        {
          AddChild(node);
        }
      }
    }

    public Node2D GetAndUpdateGroupNode(string id, float currentBeat)
    {
      if (string.IsNullOrEmpty(id) || !_groupDataMap.TryGetValue(id, out var g)) return null;

      if (Mathf.Abs(_lastUpdateBeat[id] - currentBeat) <= 0.0001f)
        return _groupNodes[id];

      if (!string.IsNullOrEmpty(g.ParentGroupID))
      {
        GetAndUpdateGroupNode(g.ParentGroupID, currentBeat);
      }

      var node = _groupNodes[id];
      var cursors = _cursors[id];

      float x = EvaluateProperty(g, StoryboardProperty.X, currentBeat, g.InitX, GetCursor(cursors, StoryboardProperty.X));
      float y = EvaluateProperty(g, StoryboardProperty.Y, currentBeat, g.InitY, GetCursor(cursors, StoryboardProperty.Y));
      float scale = EvaluateProperty(g, StoryboardProperty.Scale, currentBeat, 1f, GetCursor(cursors, StoryboardProperty.Scale));
      float scaleX = EvaluateProperty(g, StoryboardProperty.ScaleX, currentBeat, g.InitScaleX, GetCursor(cursors, StoryboardProperty.ScaleX));
      float scaleY = EvaluateProperty(g, StoryboardProperty.ScaleY, currentBeat, g.InitScaleY, GetCursor(cursors, StoryboardProperty.ScaleY));
      float rotation = EvaluateProperty(g, StoryboardProperty.Rotation, currentBeat, g.InitRotation, GetCursor(cursors, StoryboardProperty.Rotation));

      node.Position = new Vector2(x, y);
      node.Scale = new Vector2(scale * scaleX, scale * scaleY);
      node.RotationDegrees = rotation;

      _lastUpdateBeat[id] = currentBeat;

      return node;
    }

    private StoryboardEvaluator.Cursor GetCursor(Dictionary<StoryboardProperty, StoryboardEvaluator.Cursor> cursors, StoryboardProperty prop)
    {
      if (cursors.TryGetValue(prop, out var cursor)) return cursor;
      return null;
    }

    private float EvaluateProperty(GroupData g, StoryboardProperty propType, float beat, float defaultValue, StoryboardEvaluator.Cursor cursor)
    {
      if (g.StoryboardEvents == null || !g.StoryboardEvents.TryGetValue(propType, out var events)) return defaultValue;
      return StoryboardEvaluator.Evaluate(events, beat, new AnyValue(defaultValue), cursor).X;
    }
  }
}
