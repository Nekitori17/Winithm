using Godot;
using System.Collections.Generic;
using Winithm.Core.Behaviors;
using Winithm.Core.Data;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  public class HitFXController : Node
  {
    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);

    private NoteController _noteController;
    private readonly Dictionary<PackedScene, NodePool<HitFX>> _pools =
      new Dictionary<PackedScene, NodePool<HitFX>>();
    private readonly Dictionary<HitFX, PackedScene> _sceneByInstance =
      new Dictionary<HitFX, PackedScene>();

    public void Initialize(NoteController noteController)
    {
      _noteController = noteController;
    }

    public void RequestHitFX(string windowId, NoteData note, HitResultType resultType)
    {
      if (_noteController == null || note == null) return;
      if (!_noteController.TryGetHitFXSpawnInfo(windowId, note, out var info)) return;

      PackedScene scene = info.ResourcePack.HitFXScene;
      if (scene == null) return;

      var pool = GetPool(scene);
      HitFX fx = pool.Get();
      _sceneByInstance[fx] = scene;

      if (fx.GetParent() != info.ParentLayer)
      {
        fx.GetParent()?.RemoveChild(fx);
        info.ParentLayer.AddChild(fx);
      }

      info.ParentLayer.MoveChild(fx, info.ParentLayer.GetChildCount() - 1);
      fx.Position = info.Position;
      fx.Rotation = info.Rotation;
      fx.ZIndex = 0;
      fx.SetProgramText(info.ResourcePack.HitFXProgramText);
      fx.Play(
        resultType,
        ResolveHitFXColor(info.ResourcePack, resultType),
        info.NoteWidth,
        info.PlayerAreaSize,
        fx.DefaultDuration,
        info.ResourcePack.Config.HitFXAdditiveBlending,
        ReleaseHitFX
      );
    }

    public override void _ExitTree()
    {
      base._ExitTree();

      foreach (var pool in _pools.Values)
      {
        pool.Dispose();
      }

      _pools.Clear();
      _sceneByInstance.Clear();
    }

    private NodePool<HitFX> GetPool(PackedScene scene)
    {
      if (_pools.TryGetValue(scene, out var pool)) return pool;

      pool = new NodePool<HitFX>(
        parent: this,
        createFunc: () =>
        {
          Node node = scene.Instance();
          HitFX fx = node as HitFX;
          if (fx == null)
          {
            GD.PushError("[HitFXController] HitFX scene root must inherit Winithm.Core.Behaviors.HitFX.");
            node.QueueFree();
            fx = new HitFX();
          }
          AddChild(fx);
          return fx;
        },
        actionOnGet: fx =>
        {
          fx.Visible = true;
          fx.SetProcess(true);
        },
        actionOnRelease: fx =>
        {
          fx.Visible = false;
          fx.SetProcess(false);
          if (fx.GetParent() != this)
          {
            fx.GetParent()?.RemoveChild(fx);
            AddChild(fx);
          }
        },
        defaultCapacity: 16
      );

      _pools[scene] = pool;
      return pool;
    }

    private void ReleaseHitFX(HitFX fx)
    {
      if (fx == null || !_sceneByInstance.TryGetValue(fx, out var scene)) return;
      _sceneByInstance.Remove(fx);

      if (_pools.TryGetValue(scene, out var pool))
      {
        pool.Release(fx);
      }
    }

    private static Color ResolveHitFXColor(ResourcePack resourcePack, HitResultType resultType)
    {
      switch (resultType)
      {
        case HitResultType.Perfect:
          return resourcePack.Config.HitFXColorPerfect;
        case HitResultType.Good:
          return resourcePack.Config.HitFXColorGood;
        case HitResultType.Bad:
          return resourcePack.Config.HitFXColorBad;
        case HitResultType.Miss:
          return resourcePack.Config.HitFXColorMiss;
        default:
          return resourcePack.Config.HitFXColorPerfect;
      }
    }
  }
}
