namespace NacreCraft.Primitives;

//Vec2 two-dimensional float vector, maps to vanilla net.minecraft.world.phys.Vec2
//readonly struct value semantics, only the plane math vanilla uses; the vanilla codec is deferred until Vec3 gets one
public readonly struct Vec2 : IEquatable<Vec2>
{
    public static readonly Vec2 Zero = new(0.0f, 0.0f);
    public static readonly Vec2 One = new(1.0f, 1.0f);
    public static readonly Vec2 UnitX = new(1.0f, 0.0f);
    public static readonly Vec2 NegUnitX = new(-1.0f, 0.0f);
    public static readonly Vec2 UnitY = new(0.0f, 1.0f);
    public static readonly Vec2 NegUnitY = new(0.0f, -1.0f);
    public static readonly Vec2 Max = new(float.MaxValue, float.MaxValue);
    //Java's Float.MIN_VALUE is the smallest positive float, which is float.Epsilon here
    public static readonly Vec2 Min = new(float.Epsilon, float.Epsilon);

    public float X { get; }
    public float Y { get; }

    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Vec2 Scale(float scale) => new(X * scale, Y * scale);

    public float Dot(Vec2 other) => X * other.X + Y * other.Y;

    public Vec2 Add(Vec2 other) => new(X + other.X, Y + other.Y);

    public Vec2 Add(float value) => new(X + value, Y + value);

    //Normalized unit vector, a zero vector stays zero instead of dividing by almost nothing
    public Vec2 Normalized()
    {
        var length = Length();
        return length < 1.0E-4f ? Zero : new Vec2(X / length, Y / length);
    }

    public float Length() => (float)Math.Sqrt(X * X + Y * Y);

    public float LengthSquared() => X * X + Y * Y;

    public float DistanceToSqr(Vec2 other)
    {
        var dx = other.X - X;
        var dy = other.Y - Y;
        return dx * dx + dy * dy;
    }

    public Vec2 Negated() => new(-X, -Y);

    //Rotate counter-clockwise by the given angle in radians, maps to vanilla rotate
    public Vec2 Rotate(double angleRadians)
    {
        var cosine = (float)Math.Cos(angleRadians);
        var sine = (float)Math.Sin(angleRadians);
        return new Vec2(X * cosine - Y * sine, Y * cosine + X * sine);
    }

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public bool Equals(Vec2 other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is Vec2 v && Equals(v);

    public static bool operator ==(Vec2 left, Vec2 right) => left.Equals(right);

    public static bool operator !=(Vec2 left, Vec2 right) => !left.Equals(right);

    public override string ToString() => $"[{X}, {Y}]";
}
