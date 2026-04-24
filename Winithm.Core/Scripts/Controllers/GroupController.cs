using Godot;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Common;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  [Tool]
  public class GroupController : Node
  {
    private GroupManager _groupManager;
    private readonly Dictionary<string, Node2D> _groupNodes = new Dictionary<string, Node2D>();
    private readonly Dictionary<string, float> _lastUpdateBeat = new Dictionary<string, float>();

    public void LoadGroups(GroupManager manager)
    {
      _groupManager = manager ?? new GroupManager();

      foreach (var node in _groupNodes.Values)
      {
        if (IsInstanceValid(node)) node.QueueFree();
      }
      _groupNodes.Clear();
      _lastUpdateBeat.Clear();

      var allGroups = _groupManager.GroupCollection.Values;

      foreach (var g in allGroups)
      {
        _lastUpdateBeat[g.ID] = -1f;

        var node = new Node2D
        {
          Name = string.IsNullOrEmpty(g.ID) ? "Group" : g.ID,
          Position = new Vector2(g.InitX, g.InitY),
          Scale = new Vector2(g.InitScaleX, g.InitScaleY),
          RotationDegrees = g.InitRotation
        };

        _groupNodes[g.ID] = node;
      }

      foreach (var g in allGroups)
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

    public Node2D GetGroupNode(string id, float currentBeat)
    {
      if (string.IsNullOrEmpty(id) || !_groupManager.ContainsGroup(id)) return null;

      if (Mathf.Abs(_lastUpdateBeat[id] - currentBeat) <= 0.0001f)
        return _groupNodes[id];

      return ForceGetGroupNode(id, currentBeat, false);
    }

    public Node2D ForceGetGroupNode(string id, float currentBeat, bool _force = true)
    {
      if (string.IsNullOrEmpty(id) || !_groupManager.GroupCollection.TryGetValue(id, out var g)) return null;

      if (!string.IsNullOrEmpty(g.ParentGroupID))
      {
        if (_force) ForceGetGroupNode(g.ParentGroupID, currentBeat);
        else GetGroupNode(g.ParentGroupID, currentBeat);
      }

      var node = _groupNodes[id];

      float x = EvaluateProperty(g, StoryboardProperty.X, currentBeat, g.InitX, _force);
      float y = EvaluateProperty(g, StoryboardProperty.Y, currentBeat, g.InitY, _force);
      float scale = EvaluateProperty(g, StoryboardProperty.Scale, currentBeat, 1f, _force);
      float scaleX = EvaluateProperty(g, StoryboardProperty.ScaleX, currentBeat, g.InitScaleX, _force);
      float scaleY = EvaluateProperty(g, StoryboardProperty.ScaleY, currentBeat, g.InitScaleY, _force);
      float rotation = EvaluateProperty(g, StoryboardProperty.Rotation, currentBeat, g.InitRotation, _force);

      node.Position = new Vector2(x, y);
      node.Scale = new Vector2(scale * scaleX, scale * scaleY);
      node.RotationDegrees = rotation;

      _lastUpdateBeat[id] = currentBeat;

      return node;
    }

    private float EvaluateProperty(GroupData g, StoryboardProperty prop, double beat, float defaultValue, bool isScrubbing = true)
    {
      if (g.StoryboardEvents == null 
        || !g.StoryboardEvents.EventCollection.TryGetValue(prop, out var events)
      ) return defaultValue;

      return g.StoryboardEvents.Evaluate(prop, beat, new AnyValue(defaultValue), isScrubbing).X;
    }
  }
}
