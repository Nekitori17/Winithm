using System;
using System.Globalization;

namespace Winithm.Core.Common
{
    /// <summary>
    /// Represents a dynamic float vector parsed from pipe-separated format (X|Y|Z|W).
    /// Used for Colors, Vectors, and Shader Uniforms.
    /// Supports up to 4 dimensions.
    /// </summary>
    public struct VectorValue
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
        public int Size; // 1 to 4
        public bool IsDefault;

        public VectorValue(float x)
        {
            X = x; Y = 0; Z = 0; W = 0;
            Size = 1;
            IsDefault = false;
        }

        public VectorValue(float x, float y)
        {
            X = x; Y = y; Z = 0; W = 0;
            Size = 2;
            IsDefault = false;
        }

        public VectorValue(float x, float y, float z)
        {
            X = x; Y = y; Z = z; W = 0;
            Size = 3;
            IsDefault = false;
        }

        public VectorValue(float x, float y, float z, float w)
        {
            X = x; Y = y; Z = z; W = w;
            Size = 4;
            IsDefault = false;
        }

        /// <summary>
        /// Parse a pipe-separated vector string (e.g., "0.5|0.3|1" or "0.5").
        /// </summary>
        public static VectorValue Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new VectorValue();

            text = text.Trim();
            if (text == "-") return new VectorValue { IsDefault = true };

            string[] parts = text.Split('|');
            int count = Math.Min(parts.Length, 4);
            var result = new VectorValue();
            result.Size = count;

            if (count > 0) float.TryParse(parts[0].Trim(), NumberStyles.Float, ParserUtils.INV, out result.X);
            if (count > 1) float.TryParse(parts[1].Trim(), NumberStyles.Float, ParserUtils.INV, out result.Y);
            if (count > 2) float.TryParse(parts[2].Trim(), NumberStyles.Float, ParserUtils.INV, out result.Z);
            if (count > 3) float.TryParse(parts[3].Trim(), NumberStyles.Float, ParserUtils.INV, out result.W);

            return result;
        }

        /// <summary>
        /// Check if a string looks like a pipe-separated vector value.
        /// </summary>
        public static bool IsVectorFormat(string text)
        {
            return text != null && text.Contains("|");
        }

        /// <summary>
        /// Linearly interpolate between two VectorValues of matching dimension.
        /// </summary>
        public static VectorValue Lerp(VectorValue from, VectorValue to, float t)
        {
            int size = Math.Max(from.Size, to.Size);
            var result = new VectorValue();
            result.Size = size;

            if (size >= 1) result.X = from.X + (to.X - from.X) * t;
            if (size >= 2) result.Y = from.Y + (to.Y - from.Y) * t;
            if (size >= 3) result.Z = from.Z + (to.Z - from.Z) * t;
            if (size >= 4) result.W = from.W + (to.W - from.W) * t;

            return result;
        }

        public Godot.Color ToGodotColor(float alpha = 1f)
        {
            // By convention, if Size is 3, RGB uses X,Y,Z. If Size is 4, RGBA uses X,Y,Z,W.
            if (Size >= 4) return new Godot.Color(X, Y, Z, W);
            return new Godot.Color(X, Y, Z, alpha);
        }

        public override string ToString()
        {
            if (IsDefault) return "-";
            if (Size == 1) return ParserUtils.FormatFloat(X);
            if (Size == 2) return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}";
            if (Size == 3) return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}|{ParserUtils.FormatFloat(Z)}";
            return $"{ParserUtils.FormatFloat(X)}|{ParserUtils.FormatFloat(Y)}|{ParserUtils.FormatFloat(Z)}|{ParserUtils.FormatFloat(W)}";
        }
    }
}
