using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using Winithm.Core.Common;
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
    [Export] public float DefaultDuration = 0.75f;
    [Export] public bool UseCpuParticlesFallback = true;

    protected HitResultType ResultType { get; private set; }
    protected Color HitColor { get; private set; } = Colors.White;
    protected float NoteWidth { get; private set; }
    protected Vector2 PlayerAreaSize { get; private set; }

    private Action<HitFX> _onFinished;
    private HitFXProgram _program;
    private float _duration = 0.75f;
    private float _elapsed;
    private bool _playing;
    private bool _blendModeInitialized;
    private bool _lastAdditiveBlending;
    private bool[] _actionCompleted = new bool[0];

    public void SetProgramText(string programText)
    {
      _program = HitFXProgram.Parse(programText);
    }

    public virtual void Play(
      HitResultType resultType,
      Color color,
      float noteWidth,
      Vector2 playerAreaSize,
      float duration,
      bool additiveBlending,
      Action<HitFX> onFinished
    )
    {
      ResultType = resultType;
      HitColor = color;
      NoteWidth = noteWidth;
      PlayerAreaSize = playerAreaSize;
      _onFinished = onFinished;
      _elapsed = 0f;
      _playing = true;
      _duration = duration > 0f ? duration : DefaultDuration;

      var variables = CreateVariables();
      if (_program != null && _program.Duration != null)
      {
        _duration = Mathf.Max(0.001f, (float)_program.Duration.Evaluate(variables));
      }

      _actionCompleted = _program != null
        ? new bool[_program.Actions.Count]
        : new bool[0];

      Visible = true;
      SetProcess(true);
      if (!_blendModeInitialized || _lastAdditiveBlending != additiveBlending)
      {
        ApplyBlendMode(this, additiveBlending);
        _blendModeInitialized = true;
        _lastAdditiveBlending = additiveBlending;
      }
      StartCommonChildNodes(this);
      ApplyProgramStart(variables);
      OnHitFXStarted();
    }

    public override void _Process(float delta)
    {
      if (!_playing) return;

      _elapsed += delta;
      var variables = CreateVariables();

      ExecuteProgram(variables);
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

    private Dictionary<string, double> CreateVariables()
    {
      return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
      {
        { "noteWidth", NoteWidth },
        { "playerAreaWidth", PlayerAreaSize.x },
        { "playerAreaHeight", PlayerAreaSize.y },
        { "duration", _duration },
        { "colorR", HitColor.r },
        { "colorG", HitColor.g },
        { "colorB", HitColor.b },
        { "colorA", HitColor.a },
        { "result", (int)ResultType },
      };
    }

    private void ApplyProgramStart(Dictionary<string, double> variables)
    {
      if (_program == null) return;

      if (_program.ScaleWidth != null)
      {
        float widthScale = (float)_program.ScaleWidth.Evaluate(variables);
        Scale = Vector2.One * NoteWidth * widthScale;
      }

      foreach (var tint in _program.Tints)
      {
        Node node = ResolveNode(tint.NodePath);
        if (node == null) continue;

        string property = tint.UseSelfModulate ? "self_modulate" : "modulate";
        TrySetProperty(node, property, HitColor);
      }
    }

    private void ExecuteProgram(Dictionary<string, double> variables)
    {
      if (_program == null) return;

      for (int i = 0; i < _program.Actions.Count; i++)
      {
        var action = _program.Actions[i];
        if (_actionCompleted[i] && action.Type != HitFXActionType.Animate) continue;

        switch (action.Type)
        {
          case HitFXActionType.Set:
            if (_elapsed >= action.AtExpression.Evaluate(variables))
            {
              SetTargetProperty(action, action.From.Evaluate(variables));
              _actionCompleted[i] = true;
            }
            break;
          case HitFXActionType.Animate:
            ExecuteAnimate(action, variables, i);
            break;
          case HitFXActionType.Emit:
            if (_elapsed >= action.AtExpression.Evaluate(variables))
            {
              EmitParticles(action.NodePath);
              _actionCompleted[i] = true;
            }
            break;
          case HitFXActionType.Play:
            if (_elapsed >= action.AtExpression.Evaluate(variables))
            {
              PlayAnimation(action.NodePath, action.AnimationName);
              _actionCompleted[i] = true;
            }
            break;
          case HitFXActionType.Finish:
            if (_elapsed >= action.AtExpression.Evaluate(variables))
            {
              _actionCompleted[i] = true;
              Finish();
            }
            break;
        }
      }
    }

    private void ExecuteAnimate(HitFXAction action, Dictionary<string, double> variables, int actionIndex)
    {
      float start = (float)action.DelayExpression.Evaluate(variables);
      float actionDuration = Mathf.Max(0.001f, (float)action.DurationExpression.Evaluate(variables));
      float end = start + actionDuration;
      if (_elapsed < start) return;

      double value;
      if (_elapsed >= end)
      {
        value = action.To.Evaluate(variables);
        _actionCompleted[actionIndex] = true;
      }
      else
      {
        double t = (_elapsed - start) / actionDuration;
        double eased = EasingFunctions.Evaluate(action.Easing, t);
        double from = action.From.Evaluate(variables);
        double to = action.To.Evaluate(variables);
        value = from + (to - from) * eased;
      }

      SetTargetProperty(action, value);
    }

    private void SetTargetProperty(HitFXAction action, double value)
    {
      Node node = ResolveNode(action.NodePath);
      if (node == null) return;

      TrySetProperty(node, action.PropertyName, (float)value);
    }

    private Node ResolveNode(string nodePath)
    {
      if (string.IsNullOrWhiteSpace(nodePath) || nodePath == ".") return this;
      return HasNode(nodePath) ? GetNode(nodePath) : null;
    }

    private void EmitParticles(string nodePath)
    {
      Node node = ResolveNode(nodePath);
      if (node == null) return;

      node.Call("set", "emitting", false);
      if (node.HasMethod("restart")) node.Call("restart");
      node.Call("set", "emitting", true);
    }

    private void PlayAnimation(string nodePath, string animationName)
    {
      Node node = ResolveNode(nodePath);
      if (node is AnimationPlayer animationPlayer)
      {
        animationPlayer.Play(animationName);
      }
    }

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
          material = material.Duplicate() as CanvasItemMaterial;
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

    private static void TrySetProperty(Godot.Object target, string propertyName, object value)
    {
      try
      {
        target.Set(propertyName, value);
      }
      catch
      {
        GD.PushWarning($"[HitFX] Failed to set property '{propertyName}' on '{target}'.");
      }
    }

    private class HitFXProgram
    {
      public Expression Duration;
      public Expression ScaleWidth;
      public readonly List<HitFXTint> Tints = new List<HitFXTint>();
      public readonly List<HitFXAction> Actions = new List<HitFXAction>();

      public static HitFXProgram Parse(string text)
      {
        var program = new HitFXProgram();
        if (string.IsNullOrWhiteSpace(text)) return program;

        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (string rawLine in lines)
        {
          string line = StripComment(rawLine).Trim();
          if (string.IsNullOrWhiteSpace(line)) continue;

          try
          {
            ParseLine(program, line);
          }
          catch (Exception ex)
          {
            GD.PushWarning($"[HitFX] Failed to parse DSL line '{line}': {ex.Message}");
          }
        }

        return program;
      }

      private static string StripComment(string line)
      {
        int idx = line.IndexOf('#');
        return idx >= 0 ? line.Substring(0, idx) : line;
      }

      private static void ParseLine(HitFXProgram program, string line)
      {
        if (line.StartsWith("duration ", StringComparison.OrdinalIgnoreCase))
        {
          program.Duration = Expression.Parse(line.Substring("duration ".Length));
          return;
        }

        if (line.StartsWith("scale_width ", StringComparison.OrdinalIgnoreCase))
        {
          program.ScaleWidth = Expression.Parse(line.Substring("scale_width ".Length));
          return;
        }

        if (line.StartsWith("tint ", StringComparison.OrdinalIgnoreCase))
        {
          string[] parts = SplitWords(line);
          if (parts.Length < 2) throw new FormatException("tint requires a NodePath.");
          bool self = parts.Length >= 3 && string.Equals(parts[2], "self_modulate", StringComparison.OrdinalIgnoreCase);
          program.Tints.Add(new HitFXTint { NodePath = parts[1], UseSelfModulate = self });
          return;
        }

        if (line.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
        {
          ParseSet(program, line.Substring("set ".Length));
          return;
        }

        if (line.StartsWith("animate ", StringComparison.OrdinalIgnoreCase))
        {
          ParseAnimate(program, line.Substring("animate ".Length));
          return;
        }

        if (line.StartsWith("emit ", StringComparison.OrdinalIgnoreCase))
        {
          string body = line.Substring("emit ".Length);
          int atIdx = FindToken(body, " at ");
          program.Actions.Add(new HitFXAction
          {
            Type = HitFXActionType.Emit,
            NodePath = body.Substring(0, atIdx).Trim(),
            AtExpression = Expression.Parse(body.Substring(atIdx + 4))
          });
          return;
        }

        if (line.StartsWith("play ", StringComparison.OrdinalIgnoreCase))
        {
          string[] parts = SplitWords(line);
          if (parts.Length < 5 || !string.Equals(parts[3], "at", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("play syntax: play <AnimationPlayerPath> <animationName> at <time>");
          program.Actions.Add(new HitFXAction
          {
            Type = HitFXActionType.Play,
            NodePath = parts[1],
            AnimationName = parts[2],
            AtExpression = Expression.Parse(parts[4])
          });
          return;
        }

        if (line.StartsWith("finish at ", StringComparison.OrdinalIgnoreCase))
        {
          program.Actions.Add(new HitFXAction
          {
            Type = HitFXActionType.Finish,
            AtExpression = Expression.Parse(line.Substring("finish at ".Length))
          });
        }
      }

      private static void ParseSet(HitFXProgram program, string body)
      {
        int equalsIdx = body.IndexOf('=');
        if (equalsIdx < 0) throw new FormatException("set requires '='.");

        string target = body.Substring(0, equalsIdx).Trim();
        string rhs = body.Substring(equalsIdx + 1).Trim();
        int atIdx = FindToken(rhs, " at ");

        SplitTarget(target, out string nodePath, out string propertyName);
        program.Actions.Add(new HitFXAction
        {
          Type = HitFXActionType.Set,
          NodePath = nodePath,
          PropertyName = propertyName,
          From = Expression.Parse(rhs.Substring(0, atIdx)),
          AtExpression = Expression.Parse(rhs.Substring(atIdx + 4))
        });
      }

      private static void ParseAnimate(HitFXProgram program, string body)
      {
        int fromIdx = FindToken(body, " from ");
        int toIdx = FindToken(body, " to ", fromIdx + 6);
        int delayIdx = FindToken(body, " delay ", toIdx + 4);
        int durationIdx = FindToken(body, " duration ", delayIdx + 7);
        int easingIdx = FindToken(body, " easing ", durationIdx + 10);

        string target = body.Substring(0, fromIdx).Trim();
        SplitTarget(target, out string nodePath, out string propertyName);

        program.Actions.Add(new HitFXAction
        {
          Type = HitFXActionType.Animate,
          NodePath = nodePath,
          PropertyName = propertyName,
          From = Expression.Parse(body.Substring(fromIdx + 6, toIdx - fromIdx - 6)),
          To = Expression.Parse(body.Substring(toIdx + 4, delayIdx - toIdx - 4)),
          DelayExpression = Expression.Parse(body.Substring(delayIdx + 7, durationIdx - delayIdx - 7)),
          DurationExpression = Expression.Parse(body.Substring(durationIdx + 10, easingIdx - durationIdx - 10)),
          Easing = EasingFunctions.ParseEasing(body.Substring(easingIdx + 8).Trim())
        });
      }

      private static int FindToken(string text, string token, int startIndex = 0)
      {
        int idx = text.IndexOf(token, startIndex, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) throw new FormatException($"Missing token '{token.Trim()}'.");
        return idx;
      }

      private static void SplitTarget(string target, out string nodePath, out string propertyName)
      {
        int dotIdx = target.LastIndexOf('.');
        if (dotIdx < 0) throw new FormatException("Target syntax must be <NodePath>.<Property>.");
        nodePath = target.Substring(0, dotIdx).Trim();
        propertyName = target.Substring(dotIdx + 1).Trim();
      }

      private static string[] SplitWords(string line)
      {
        return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
      }
    }

    private class HitFXTint
    {
      public string NodePath;
      public bool UseSelfModulate;
    }

    private enum HitFXActionType
    {
      Set,
      Animate,
      Emit,
      Play,
      Finish
    }

    private class HitFXAction
    {
      public HitFXActionType Type;
      public string NodePath;
      public string PropertyName;
      public string AnimationName;
      public Expression From;
      public Expression To;
      public Expression AtExpression;
      public Expression DelayExpression;
      public Expression DurationExpression;
      public EasingType Easing;
    }

    private class Expression
    {
      private readonly string _text;

      private Expression(string text)
      {
        _text = text ?? "0";
      }

      public static Expression Parse(string text) => new Expression(text);

      public double Evaluate(Dictionary<string, double> variables)
      {
        var parser = new ExpressionParser(_text, variables);
        return parser.Parse();
      }
    }

    private class ExpressionParser
    {
      private readonly string _text;
      private readonly Dictionary<string, double> _variables;
      private int _index;

      public ExpressionParser(string text, Dictionary<string, double> variables)
      {
        _text = text ?? "0";
        _variables = variables;
      }

      public double Parse()
      {
        double value = ParseAddSub();
        SkipWhitespace();
        return value;
      }

      private double ParseAddSub()
      {
        double value = ParseMulDiv();
        while (true)
        {
          SkipWhitespace();
          if (TryConsume('+')) value += ParseMulDiv();
          else if (TryConsume('-')) value -= ParseMulDiv();
          else return value;
        }
      }

      private double ParseMulDiv()
      {
        double value = ParseUnary();
        while (true)
        {
          SkipWhitespace();
          if (TryConsume('*')) value *= ParseUnary();
          else if (TryConsume('/')) value /= ParseUnary();
          else return value;
        }
      }

      private double ParseUnary()
      {
        SkipWhitespace();
        if (TryConsume('-')) return -ParseUnary();
        if (TryConsume('+')) return ParseUnary();
        return ParsePrimary();
      }

      private double ParsePrimary()
      {
        SkipWhitespace();
        if (TryConsume('('))
        {
          double value = ParseAddSub();
          TryConsume(')');
          return value;
        }

        if (_index < _text.Length && (char.IsLetter(_text[_index]) || _text[_index] == '_'))
        {
          string identifier = ParseIdentifier();
          if (_variables != null && _variables.TryGetValue(identifier, out double value)) return value;
          return 0d;
        }

        return ParseNumber();
      }

      private string ParseIdentifier()
      {
        int start = _index;
        while (_index < _text.Length && (char.IsLetterOrDigit(_text[_index]) || _text[_index] == '_'))
        {
          _index++;
        }
        return _text.Substring(start, _index - start);
      }

      private double ParseNumber()
      {
        int start = _index;
        while (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] == '.'))
        {
          _index++;
        }

        if (start == _index) return 0d;
        string number = _text.Substring(start, _index - start);
        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
          ? value
          : 0d;
      }

      private bool TryConsume(char c)
      {
        SkipWhitespace();
        if (_index >= _text.Length || _text[_index] != c) return false;
        _index++;
        return true;
      }

      private void SkipWhitespace()
      {
        while (_index < _text.Length && char.IsWhiteSpace(_text[_index])) _index++;
      }
    }
  }
}
