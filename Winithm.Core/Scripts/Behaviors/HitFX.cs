using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Data;
using Winithm.Core.Interfaces;

namespace Winithm.Core.Behaviors
{
  /// <summary>
  /// Generic runtime contract for resource-pack HitFX scenes.
  /// Visual shapes and pack-specific animation belong in the scene, DSL, or derived scripts.
  /// </summary>
  public class HitFX : Node2D, IPoolable
  {
    [Export] public float DefaultDuration = 0.5f;
    [Export] public bool UseCpuParticlesFallback = true;

    protected HitResultType ResultType { get; private set; }
    protected NoteType NoteType { get; private set; }
    protected Color HitColor { get; set; } = Colors.White;
    protected float NoteWidth { get; private set; }
    protected Vector2 PlayerAreaSize { get; private set; }
    protected float Elapsed => _elapsed;

    private Action<HitFX> _onFinished;
    private float _duration = 0.5f;
    private float _elapsed;
    private bool _playing;
    private bool _blendModeInitialized;
    private bool _lastAdditiveBlending;

    public virtual void Play(
      HitResultType resultType,
      NoteType noteType,
      float noteWidth,
      Vector2 playerAreaSize,
      bool additiveBlending,
      Action<HitFX> onFinished
    )
    {
      ResultType = resultType;
      NoteType = noteType;
      NoteWidth = noteWidth;
      PlayerAreaSize = playerAreaSize;
      _onFinished = onFinished;
      _elapsed = 0f;
      _playing = true;
      _duration = DefaultDuration;

      Visible = true;
      SetProcess(true);
      if (!_blendModeInitialized || _lastAdditiveBlending != additiveBlending)
      {
        ApplyBlendMode(this, additiveBlending);
        _blendModeInitialized = true;
        _lastAdditiveBlending = additiveBlending;
      }
      StartCommonChildNodes(this);
      OnHitFXStarted();
    }

    public override void _Process(float delta)
    {
      if (!_playing) return;

      _elapsed += delta;
      
      OnHitFXProcess(delta);

      if (_elapsed >= _duration)
      {
        Finish();
      }
    }

    public void Stop()
    {
      Finish();
    }

    public void OnSpawn()
    {
      Visible = true;
      SetProcess(false);
    }

    public void OnDespawn()
    {
      _playing = false;
      _elapsed = 0f;
      _onFinished = null;
      StopCommonChildNodes(this);
      OnHitFXStopped();
    }

    protected virtual void OnHitFXStarted() { }
    protected virtual void OnHitFXProcess(float delta) { }
    protected virtual void OnHitFXStopped() { }

    protected void Finish()
    {
      if (!_playing) return;

      _playing = false;
      StopCommonChildNodes(this);
      OnHitFXStopped();

      var callback = _onFinished;
      _onFinished = null;
      callback?.Invoke(this);
    }

    public void SetDuration(float duration) => _duration = duration;

    private void StartCommonChildNodes(Node node)
    {
      if (node is AnimatedSprite animatedSprite) animatedSprite.Play();
      if (node is AnimationPlayer animationPlayer) animationPlayer.Play();

      if (node is Particles2D particles)
      {
        Node particleNode = UseCpuParticlesFallback ? EnsureCpuParticleFallback(particles) : particles;
        particleNode.Call("set", "emitting", false);
        if (particleNode.HasMethod("restart")) particleNode.Call("restart");
      }
      else if (node is CPUParticles2D cpuParticles)
      {
        cpuParticles.Emitting = false;
        cpuParticles.Restart();
      }

      for (int i = 0; i < node.GetChildCount(); i++)
      {
        StartCommonChildNodes(node.GetChild(i));
      }
    }

    private static void ApplyBlendMode(Node node, bool additiveBlending)
    {
      if (node is CanvasItem canvasItem)
      {
        CanvasItemMaterial material = canvasItem.Material as CanvasItemMaterial;
        if (material == null)
        {
          material = new CanvasItemMaterial();
          canvasItem.Material = material;
        }
        else
        {
          canvasItem.Material = material;
        }

        material.BlendMode = additiveBlending
          ? CanvasItemMaterial.BlendModeEnum.Add
          : CanvasItemMaterial.BlendModeEnum.Mix;
      }

      for (int i = 0; i < node.GetChildCount(); i++)
      {
        ApplyBlendMode(node.GetChild(i), additiveBlending);
      }
    }

    private void StopCommonChildNodes(Node node)
    {
      if (node is AnimatedSprite animatedSprite) animatedSprite.Stop();
      if (node is AnimationPlayer animationPlayer) animationPlayer.Stop();

      if (node is Particles2D || node is CPUParticles2D)
      {
        node.Call("set", "emitting", false);
      }

      for (int i = 0; i < node.GetChildCount(); i++)
      {
        StopCommonChildNodes(node.GetChild(i));
      }
    }

    private Node EnsureCpuParticleFallback(Particles2D particles)
    {
      string driver = ProjectSettings.GetSetting("rendering/quality/driver/driver_name") as string;
      if (!string.Equals(driver, "GLES2", StringComparison.OrdinalIgnoreCase)) return particles;

      string fallbackName = particles.Name + "_CPUFallback";
      Node parent = particles.GetParent();
      if (parent == null) return particles;

      if (parent.HasNode(fallbackName))
      {
        particles.Visible = false;
        return parent.GetNode(fallbackName);
      }

      var cpuParticles = new CPUParticles2D { Name = fallbackName };
      parent.AddChild(cpuParticles);
      parent.MoveChild(cpuParticles, particles.GetIndex() + 1);
      CopyCommonProperties(particles, cpuParticles);

      if (cpuParticles.HasMethod("convert_from_particles"))
      {
        cpuParticles.Call("convert_from_particles", particles);
      }

      particles.Visible = false;
      return cpuParticles;
    }

    private static void CopyCommonProperties(Godot.Object source, Godot.Object target)
    {
      var targetProperties = new HashSet<string>();
      foreach (Godot.Collections.Dictionary property in target.GetPropertyList())
      {
        if (property.Contains("name")) targetProperties.Add((string)property["name"]);
      }

      foreach (Godot.Collections.Dictionary property in source.GetPropertyList())
      {
        if (!property.Contains("name")) continue;
        string name = (string)property["name"];
        if (!targetProperties.Contains(name)) continue;

        try
        {
          target.Set(name, source.Get(name));
        }
        catch
        {
          // Best effort. Some engine properties are read-only or type-incompatible.
        }
      }
    }
  }
}
