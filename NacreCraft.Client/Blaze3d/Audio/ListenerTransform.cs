using NacreCraft.Primitives;

namespace NacreCraft.Client.Blaze3d.Audio;

//ListenerTransform the listener position and orientation, maps to vanilla com.mojang.blaze3d.audio.ListenerTransform
public sealed record ListenerTransform(Vec3 Position, Vec3 Forward, Vec3 Up)
{
    public static readonly ListenerTransform Initial = new(Vec3.Zero, new Vec3(0.0, 0.0, -1.0), new Vec3(0.0, 1.0, 0.0));

    public Vec3 Right() => new(
        Forward.Y * Up.Z - Forward.Z * Up.Y,
        Forward.Z * Up.X - Forward.X * Up.Z,
        Forward.X * Up.Y - Forward.Y * Up.X);
}
