using System;
using System.Globalization;

namespace Winithm.Core.Common
{
  public enum AnyValueType
  {
    Float,
    Vector2,
    Vector3,
    Vector4,
    String,
    Inherited,
    ColorRGB,
    ColorRGBA
  }

  /// <summary>
  /// Represents a universal value that can be a float, a vector up to 4D, a string, or an inherited placeholder.
  /// Formats: "0.5|0.3|1" (Vector3), "0.5" (Float), "-" (Inherited), "Hello" (String).
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
      Type = AnyValueType.Vector2;
    }

    public AnyValue(float x, float y, float z)
    {
      X = x; Y = y; Z = z; W = 0;
      StringValue = null;
      Type = AnyValueType.Vector3;
    }

    public AnyValue(float x, float y, float z, float w)
    {
      X = x; Y = y; Z = z; W = w;
      StringValue = null;
      Type = AnyValueType.Vector4;
    }

    public AnyValue(string value)
    {
      X = 0; Y = 0; Z = 0; W = 0;
      StringValue = value;
      Type = AnyValueType.String;
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
        AnyValue result = new AnyValue { Type = (AnyValueType)(count - 1) }; // Float=0, Vector2=1, Vector3=2, Vector4=3

        if (count > 0) float.TryParse(parts[0].Trim(), NumberStyles.Float, ParserUtils.INV, out result.X);
        if (count > 1) float.TryParse(parts[1].Trim(), NumberStyles.Float, ParserUtils.INV, out result.Y);
        if (count > 2) float.TryParse(parts[2].Trim(), NumberStyles.Float, ParserUtils.INV, out result.Z);
        if (count > 3) float.TryParse(parts[3].Trim(), NumberStyles.Float, ParserUtils.INV, out result.W);
        return result;
      }

      // Plain number
      if (float.TryParse(text, NumberStyles.Float, ParserUtils.INV, out float fValue))
      {
        return new AnyValue(fValue);
      }

      // String
      return new AnyValue(text.Trim('\"'));
    }

    /// <summary>
    /// Linearly interpolate between two identical-typed numeric/vector AnyValues.
    /// If types are strings or inherited, or don't match exactly in numeric mode, returns 'from'. (Or implements snap).
    /// </summary>
    public static AnyValue Lerp(AnyValue from, AnyValue to, float t)
    {
      if (from.Type == AnyValueType.String || to.Type == AnyValueType.String || 
          from.Type == AnyValueType.Inherited || to.Type == AnyValueType.Inherited)
      {
        return t >= 1f ? to : from; // Snap for strings and inherited
      }

      int sizeFrom = (int)from.Type + 1;
      int sizeTo = (int)to.Type + 1;
      int maxSize = Math.Max(sizeFrom, sizeTo);

      AnyValue result = new AnyValue();
      
      // Determine resulting type hint
      if (from.Type == AnyValueType.ColorRGBA || to.Type == AnyValueType.ColorRGBA) result.Type = AnyValueType.ColorRGBA;
      else if (from.Type == AnyValueType.ColorRGB || to.Type == AnyValueType.ColorRGB) result.Type = AnyValueType.ColorRGB;
      else result.Type = (AnyValueType)Math.Min(maxSize - 1, 3);

      if (maxSize >= 1) result.X = from.X + (to.X - from.X) * t;
      if (maxSize >= 2) result.Y = from.Y + (to.Y - from.Y) * t;
      if (maxSize >= 3) result.Z = from.Z + (to.Z - from.Z) * t;
      if (maxSize >= 4) result.W = from.W + (to.W - from.W) * t;

      return result;
    }

    public Godot.Color ToGodotColor(float alpha = 1f)
    {
      if (Type == AnyValueType.Vector4) return new Godot.Color(X, Y, Z, W);
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
          if (StringValue != null && StringValue.Contains(" ")) return $"\"{StringValue}\""; // Quote if it contains spaces
          return StringValue ?? "";
        case AnyValueType.Float: return ParserUtils.FormatFloat(X);
        case AnyValueType.Vector2: return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}";
        case AnyValueType.Vector3:
        case AnyValueType.ColorRGB: return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}|{ParserUtils.FormatFloat(Z)}";
        case AnyValueType.Vector4:
        case AnyValueType.ColorRGBA: return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}|{ParserUtils.FormatFloat(Z)}|{ParserUtils.FormatFloat(W)}";
        default: return "";
      }
    }
  }
}
