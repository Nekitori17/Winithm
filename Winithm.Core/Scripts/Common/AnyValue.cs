using System;
using System.Globalization;

namespace Winithm.Core.Common
{
  /// <summary>
  /// Supported value types, aligned with GLSL data types.
  /// </summary>
  public enum AnyValueType
  {
    Float,
    Vec2,
    Vec3,
    Vec4,
    Bool,
    String,
    Inherited
  }

  /// <summary>
  /// Represents a universal value that can be a float, a vector up to 4D, an int, a string, or an inherited placeholder.
  /// Formats: "0.5|0.3|1" (Vec3), "0.5" (Float), "-" (Inherited), "Hello" (String).
  /// </summary>
  public struct AnyValue
  {
    public float X;
    public float Y;
    public float Z;
    public float W;
    public string StringValue;
    public AnyValueType Type;

    public AnyValue(float x)
    {
      X = x; Y = 0; Z = 0; W = 0;
      StringValue = null;
      Type = AnyValueType.Float;
    }

    public AnyValue(float x, float y)
    {
      X = x; Y = y; Z = 0; W = 0;
      StringValue = null;
      Type = AnyValueType.Vec2;
    }

    public AnyValue(float x, float y, float z)
    {
      X = x; Y = y; Z = z; W = 0;
      StringValue = null;
      Type = AnyValueType.Vec3;
    }

    public AnyValue(float x, float y, float z, float w)
    {
      X = x; Y = y; Z = z; W = w;
      StringValue = null;
      Type = AnyValueType.Vec4;
    }


    public AnyValue(string value)
    {
      X = 0; Y = 0; Z = 0; W = 0;
      StringValue = value;
      Type = AnyValueType.String;
    }

    public AnyValue(bool value)
    {
      X = value ? 1 : 0; Y = 0; Z = 0; W = 0;
      StringValue = null;
      Type = AnyValueType.Bool;
    }

    /// <summary>
    /// Returns the number of float components for a given type.
    /// </summary>
    public static int ComponentCount(AnyValueType type)
    {
      switch (type)
      {
        case AnyValueType.Float:
        case AnyValueType.Bool: return 1;
        case AnyValueType.Vec2: return 2;
        case AnyValueType.Vec3: return 3;
        case AnyValueType.Vec4: return 4;
        default: return 0;
      }
    }

    /// <summary>
    /// Parses a string into the most appropriate AnyValue representation.
    /// Uses | as the vector delimiter.
    /// </summary>
    public static AnyValue Parse(string text)
    {
      if (string.IsNullOrWhiteSpace(text)) return new AnyValue("");

      text = text.Trim();

      // Inheritance marker
      if (text == "-") return new AnyValue { Type = AnyValueType.Inherited };

      // Pipe-separated vector
      if (text.Contains("|"))
      {
        string[] parts = text.Split('|');
        int count = Math.Min(parts.Length, 4);

        AnyValueType[] vecTypes = { AnyValueType.Float, AnyValueType.Vec2, AnyValueType.Vec3, AnyValueType.Vec4 };
        AnyValue result = new AnyValue { Type = vecTypes[Math.Min(count - 1, 3)] };

        if (count > 0) float.TryParse(parts[0].Trim(), NumberStyles.Float, ParserUtils.INV, out result.X);
        if (count > 1) float.TryParse(parts[1].Trim(), NumberStyles.Float, ParserUtils.INV, out result.Y);
        if (count > 2) float.TryParse(parts[2].Trim(), NumberStyles.Float, ParserUtils.INV, out result.Z);
        if (count > 3) float.TryParse(parts[3].Trim(), NumberStyles.Float, ParserUtils.INV, out result.W);
        return result;
      }

      // Boolean
      if (ParserUtils.TryParseBool(text, out bool bValue))
      {
        return new AnyValue(bValue);
      }

      // Plain number (float)
      if (ParserUtils.TryParseFloat(text, out float fValue))
      {
        return new AnyValue(fValue);
      }

      // String
      return new AnyValue(text.Trim('\"'));
    }

    /// <summary>
    /// Linearly interpolate between two numeric/vector AnyValues.
    /// Non-interpolatable types (String, Inherited, Bool) snap at t >= 1.
    /// </summary>
    public static AnyValue Lerp(AnyValue from, AnyValue to, float t)
    {
      if (
        from.Type == AnyValueType.String ||
        to.Type == AnyValueType.String ||
        from.Type == AnyValueType.Inherited ||
        to.Type == AnyValueType.Inherited ||
        from.Type == AnyValueType.Bool ||
        to.Type == AnyValueType.Bool)
      {
        return t >= 1f ? to : from;
      }

      int sizeFrom = ComponentCount(from.Type);
      int sizeTo = ComponentCount(to.Type);
      int maxSize = Math.Max(sizeFrom, sizeTo);

      AnyValue result = new AnyValue();

      // Determine resulting type based on max component count
      switch (maxSize)
      {
        case 1: result.Type = AnyValueType.Float; break;
        case 2: result.Type = AnyValueType.Vec2; break;
        case 3: result.Type = AnyValueType.Vec3; break;
        case 4: result.Type = AnyValueType.Vec4; break;
        default: result.Type = AnyValueType.Float; break;
      }

      if (maxSize >= 1) result.X = from.X + (to.X - from.X) * t;
      if (maxSize >= 2) result.Y = from.Y + (to.Y - from.Y) * t;
      if (maxSize >= 3) result.Z = from.Z + (to.Z - from.Z) * t;
      if (maxSize >= 4) result.W = from.W + (to.W - from.W) * t;

      return result;
    }

    public Godot.Color ToGodotColor(float alpha = 1f)
    {
      if (Type == AnyValueType.Vec4) return new Godot.Color(X, Y, Z, W);
      return new Godot.Color(X, Y, Z, alpha);
    }

    public Godot.Vector2 ToGodotVector2() => new Godot.Vector2(X, Y);
    public Godot.Vector3 ToGodotVector3() => new Godot.Vector3(X, Y, Z);

    public override string ToString()
    {
      switch (Type)
      {
        case AnyValueType.Inherited: return "-";
        case AnyValueType.String:
          if (StringValue != null && StringValue.Contains(" ")) return $"\"{StringValue}\"";
          return StringValue ?? "";
        case AnyValueType.Bool: return X == 1 ? "1" : "0";
        case AnyValueType.Float: return ParserUtils.FormatFloat(X);
        case AnyValueType.Vec2: return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}";
        case AnyValueType.Vec3: return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}|{ParserUtils.FormatFloat(Z)}";
        case AnyValueType.Vec4: return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}|{ParserUtils.FormatFloat(Z)}|{ParserUtils.FormatFloat(W)}";
        default: return "";
      }
    }
  }
}
