using System.Globalization;

namespace Winithm.Core.Common
{
    /// <summary>
    /// Generic shader vector parsed from pipe-separated format.
    /// Supports vec2 (X|Y), vec3 (X|Y|Z), and vec4 (X|Y|Z|W).
    /// The engine auto-detects vector dimension from the shader's uniform declarations.
    /// </summary>
    public struct ShaderVector
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
        public int Dimension;

        public ShaderVector(float x = 0f, float y = 0f, float z = 0f, float w = 0f, int dimension = 4)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
            Dimension = dimension;
        }

        /// <summary>
        /// Parse a pipe-separated vector string (e.g., "0.5|0.3|1" → vec3, "1|0|0|1" → vec4).
        /// </summary>
        public static ShaderVector Parse(string text)
        {
            text = text.Trim();
            string[] parts = text.Split('|');
            var v = new ShaderVector();
            v.Dimension = parts.Length;

            if (parts.Length >= 1) v.X = ParseComp(parts[0]);
            if (parts.Length >= 2) v.Y = ParseComp(parts[1]);
            if (parts.Length >= 3) v.Z = ParseComp(parts[2]);
            if (parts.Length >= 4) v.W = ParseComp(parts[3]);

            return v;
        }

        /// <summary>
        /// Check if a string is pipe-separated vector format.
        /// </summary>
        public static bool IsVectorFormat(string text)
        {
            return text != null && text.Contains("|");
        }

        /// <summary>
        /// Linearly interpolate between two ShaderVectors.
        /// </summary>
        public static ShaderVector Lerp(ShaderVector from, ShaderVector to, float t)
        {
            int dim = from.Dimension > to.Dimension ? from.Dimension : to.Dimension;
            return new ShaderVector(
                from.X + (to.X - from.X) * t,
                from.Y + (to.Y - from.Y) * t,
                from.Z + (to.Z - from.Z) * t,
                from.W + (to.W - from.W) * t,
                dim
            );
        }

        public Godot.Color ToGodotColor()
        {
            return new Godot.Color(X, Y, Z, Dimension >= 4 ? W : 1f);
        }

        public Godot.Vector2 ToGodotVector2()
        {
            return new Godot.Vector2(X, Y);
        }

        public Godot.Vector3 ToGodotVector3()
        {
            return new Godot.Vector3(X, Y, Z);
        }

        private static float ParseComp(string s)
        {
            float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v);
            return v;
        }

        public override string ToString()
        {
            switch (Dimension)
            {
                case 2: return $"{X}|{Y}";
                case 3: return $"{X}|{Y}|{Z}";
                default: return $"{X}|{Y}|{Z}|{W}";
            }
        }
    }
}
