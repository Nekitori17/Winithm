using System;
using Godot;

namespace Winithm.Core.Managers
{
  /// <summary>
  /// A Godot-specific Object Pool that automatically handles scene instantiation, 
  /// toggling visibility/processing, and queueing free on destroy.
  /// </summary>
  public class NodePool<T> : ObjectPool<T> where T : Node
  {
    /// <summary>
    /// Instantiates a Godot Node pool.
    /// </summary>
    /// <param name="parent">The parent Node that all pooled objects will be added to upon creation.</param>
    /// <param name="scene">Optional: The PackedScene to instantiate. If null, it will default to new T().</param>
    /// <param name="createFunc">Optional override for creation logic.</param>
    /// <param name="actionOnGet">Optional override for get logic. Defaults to making the node visible and enabling processing.</param>
    /// <param name="actionOnRelease">Optional override for release logic. Defaults to making the node invisible and disabling processing.</param>
    /// <param name="actionOnDestroy">Optional override for destroy logic. Defaults to calling QueueFree().</param>
    /// <param name="collectionCheck">Whether to throw exceptions if the same object is returned twice.</param>
    /// <param name="defaultCapacity">Initial stack size.</param>
    /// <param name="maxSize">Max stack size (objects beyond this are queued free).</param>
    public NodePool(
        Node parent,
        PackedScene scene = null,
        Func<T> createFunc = null,
        Action<T> actionOnGet = null,
        Action<T> actionOnRelease = null,
        Action<T> actionOnDestroy = null,
        bool collectionCheck = true,
        int defaultCapacity = 10,
        int maxSize = 10000)
        : base(
            createFunc ?? (Func<T>)(() =>
            {
              T instance = scene != null ? scene.Instance<T>() : (System.Activator.CreateInstance<T>());
              // Instantly add it to the tree so it doesn't have to be re-parented later
              parent.AddChild(instance);
              return instance;
            }),
            actionOnGet ?? (Action<T>)(element =>
            {
              // Toggling visibility and processing is significantly faster than RemoveChild() and AddChild()
              if (element is CanvasItem canvasItem) canvasItem.Visible = true;
              if (element is Spatial spatial) spatial.Visible = true;

              element.SetProcess(true);
              element.SetPhysicsProcess(true);
            }),
            actionOnRelease ?? (Action<T>)(element =>
            {
              if (element is CanvasItem canvasItem) canvasItem.Visible = false;
              if (element is Spatial spatial) spatial.Visible = false;

              element.SetProcess(false);
              element.SetPhysicsProcess(false);
            }),
            actionOnDestroy ?? (Action<T>)(element =>
            {
              if (Godot.Object.IsInstanceValid(element) && !element.IsQueuedForDeletion())
              {
                element.QueueFree();
              }
            }),
            collectionCheck,
            defaultCapacity,
            maxSize)
    {
    }
  }
}
