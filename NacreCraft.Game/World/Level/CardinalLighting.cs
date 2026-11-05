using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level;

//CardinalLighting per-face shading factors, maps to vanilla net.minecraft.world.level.CardinalLighting
//The defaults match the overworld and nether profiles; the type enum carries the serialized name used by dimension types
public sealed record CardinalLighting(
    float Down,
    float Up,
    float North,
    float South,
    float West,
    float East)
{
    public static readonly CardinalLighting Default = new(0.5f, 1.0f, 0.8f, 0.8f, 0.6f, 0.6f);

    public static readonly CardinalLighting Nether = new(0.9f, 0.9f, 0.8f, 0.8f, 0.6f, 0.6f);

    //ByFace shading factor of one face, maps to vanilla byFace
    public float ByFace(Direction direction)
    {
        if (direction.Equals(Direction.Down)) return Down;
        if (direction.Equals(Direction.Up)) return Up;
        if (direction.Equals(Direction.North)) return North;
        if (direction.Equals(Direction.South)) return South;
        if (direction.Equals(Direction.West)) return West;
        if (direction.Equals(Direction.East)) return East;
        throw new ArgumentOutOfRangeException(nameof(direction));
    }

    //Type named lighting profile, maps to vanilla CardinalLighting.Type
    public enum Type
    {
        Default,
        Nether,
    }

    //Get the profile values of a named type
    public static CardinalLighting Get(Type type) => type switch
    {
        Type.Nether => Nether,
        _ => Default,
    };

    //GetSerializedName the name used by dimension types, maps to vanilla getSerializedName
    public static string GetSerializedName(Type type) => type switch
    {
        Type.Nether => "nether",
        _ => "default",
    };
}
