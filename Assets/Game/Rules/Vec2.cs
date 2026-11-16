using System;

namespace LittleKeep.Rules
{
    /// A 2D point on the lane. x runs from the Keep (0) towards where Heroes enter; y runs across the lane.
    [Serializable]
    public struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y) { X = x; Y = y; }

        public float Length => (float)Math.Sqrt(X * X + Y * Y);
        public Vec2 Normalized { get { float l = Length; return l < 1e-6f ? new Vec2(1f, 0f) : new Vec2(X / l, Y / l); } }
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);

        public Vec2 Rotated(float degrees)
        {
            double r = degrees * Math.PI / 180.0;
            float c = (float)Math.Cos(r), s = (float)Math.Sin(r);
            return new Vec2(X * c - Y * s, X * s + Y * c);
        }

        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }
}
