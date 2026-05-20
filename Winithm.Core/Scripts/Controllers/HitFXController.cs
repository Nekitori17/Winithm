using Godot;
using System.Collections.Generic;
using Winithm.Core.Behaviors;
using Winithm.Core.Data;
using Winithm.Core.Managers;

namespace Winithm.Core.Controllers
{
  public class HitFXController : Node
  {
    private Control _hitFXLayer;

    [Export] public Vector2 PlayerAreaSize = new Vector2(1280, 720);

    private NoteController _noteController;
    private readonly Dictionary<PackedScene, NodePool<HitFX>> _pools =
      new Dictionary<PackedScene, NodePool<HitFX>>();
    private readonly Dictionary<HitFX, PackedScene> _sceneByInstance =
      new Dictionary<HitFX, PackedScene>();

    public struct HitFXSpawnInfo
    {
      public Vector2 Position;
      public float Rotation;
      public float NoteWidth;
      public Vector2 PlayerAreaSize;
      public ResourcePack ResourcePack;
    }

    public void Initialize(Control hitFXLayer, NoteController noteController)
    {
      _hitFXLayer = hitFXLayer;
      _noteController = noteController;
    }

    public void RequestHitFX(string windowId, NoteData note, HitResultType resultType)
    {
      if (_noteController == null || note == null || _hitFXLayer == null) return;
      if (!_noteController.TryGetHitFXSpawnInfo(windowId, note, out var info)) return;

      PackedScene scene = info.ResourcePack.HitFXScene;
      if (scene == null) return;

      var pool = GetPool(scene);
      HitFX fx = pool.Get();
      _sceneByInstance[fx] = scene;

      // Parent to the global HitFXLayer instead of window's local ParentLayer
      if (fx.GetParent() != _hitFXLayer)
      {
        fx.GetParent()?.RemoveChild(fx);
        _hitFXLayer.AddChild(fx);
      }

      _hitFXLayer.MoveChild(fx, _hitFXLayer.GetChildCount() - 1);

      // Convert global coordinate to global _hitFXLayer local space
      fx.Position = _hitFXLayer.GetGlobalTransform().AffineInverse() * info.Position;
      fx.Rotation = info.Rotation;
      fx.ZIndex = 0;

      fx.Play(
        resultType,
        note.Type,
        info.NoteWidth,
        info.PlayerAreaSize,
        info.ResourcePack.Config.HitFXAdditiveBlending,
        ReleaseHitFX
      );
    }

    public void Prewarm(ResourcePack resourcePack)
    {
      if (resourcePack.HitFXScene != null)
      {
        var pool = GetPool(resourcePack.HitFXScene); // Will instantiate 16 nodes instantly

        // Force shader compilation to prevent first-hit stutter
        HitFX dummy = pool.Get();
        _sceneByInstance[dummy] = resourcePack.HitFXScene;

        dummy.Position = PlayerAreaSize;
        dummy.Modulate = new Color(1f, 1f, 1f, 0.01f); // Nearly invisible to avoid flash

        dummy.Play(
          HitResultType.Perfect,
          NoteType.Tap,
          1f, // Dummy note width
          PlayerAreaSize,
          resourcePack.Config.HitFXAdditiveBlending,
          fx =>
          {
            fx.Modulate = Colors.White; // Reset modulate for actual gameplay
            ReleaseHitFX(fx);
          }
        );
      }
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
  }
}
