using NacreCraft.Commands;
using NacreCraft.Game.Commands;
using NacreCraft.Primitives;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//WorldCoordinates three-segment world coordinates, maps to vanilla WorldCoordinates
//x/y/z each have an independent relative flag, separated by spaces; a missing segment throws an incomplete error
public sealed record WorldCoordinates(WorldCoordinate X, WorldCoordinate Y, WorldCoordinate Z) : Coordinates
{
    //ZeroRotation an absolute zero rotation, maps to vanilla ZERO_ROTATION
    public static readonly WorldCoordinates ZeroRotation = Absolute(0f, 0f);

    public Vec3 GetPosition(ServerCommandSource source)
    {
        var pos = source.Position;
        return new Vec3(X.Get(pos.X), Y.Get(pos.Y), Z.Get(pos.Z));
    }

    //GetRotation aligned with vanilla Vec2(x=pitch,y=yaw); the x segment drives pitch and the y segment drives yaw
    //A source without an entity has zero rotation instead of failing like PlayerOrThrow would
    public (float Yaw, float Pitch) GetRotation(ServerCommandSource source)
    {
        var (yaw, pitch) = source.Rotation;
        return ((float)Y.Get(yaw), (float)X.Get(pitch));
    }

    public bool IsXRelative => X.IsRelative;
    public bool IsYRelative => Y.IsRelative;
    public bool IsZRelative => Z.IsRelative;

    //ParseDouble parses the three world coordinate segments; x/z apply center correction, y does not
    public static WorldCoordinates ParseDouble(StringReader reader, bool centerCorrect)
    {
        var start = reader.Cursor;
        var x = WorldCoordinate.ParseDouble(reader, centerCorrect);
        if (!reader.CanRead() || reader.Peek() != ' ')
        {
            reader.SetCursor(start);
            throw Vec3Argument.ErrorNotComplete.CreateWithContext(reader);
        }
        reader.Skip();
        var y = WorldCoordinate.ParseDouble(reader, false);
        if (!reader.CanRead() || reader.Peek() != ' ')
        {
            reader.SetCursor(start);
            throw Vec3Argument.ErrorNotComplete.CreateWithContext(reader);
        }
        reader.Skip();
        var z = WorldCoordinate.ParseDouble(reader, centerCorrect);
        return new WorldCoordinates(x, y, z);
    }

    //Absolute builds absolute coordinates
    public static WorldCoordinates Absolute(double x, double y, double z)
        => new(new WorldCoordinate(false, x), new WorldCoordinate(false, y), new WorldCoordinate(false, z));

    //Absolute builds an absolute rotation; pitch fills the x segment and yaw the y segment, maps to vanilla absolute(Vec2)
    public static WorldCoordinates Absolute(float pitch, float yaw)
        => new(new WorldCoordinate(false, pitch), new WorldCoordinate(false, yaw), new WorldCoordinate(true, 0.0));

    //ParseInt parses three integer coordinate segments, maps to vanilla parseInt
    public static WorldCoordinates ParseInt(StringReader reader)
    {
        var start = reader.Cursor;
        var x = WorldCoordinate.ParseInt(reader);
        if (!reader.CanRead() || reader.Peek() != ' ')
        {
            reader.SetCursor(start);
            throw Vec3Argument.ErrorNotComplete.CreateWithContext(reader);
        }
        reader.Skip();
        var y = WorldCoordinate.ParseInt(reader);
        if (!reader.CanRead() || reader.Peek() != ' ')
        {
            reader.SetCursor(start);
            throw Vec3Argument.ErrorNotComplete.CreateWithContext(reader);
        }
        reader.Skip();
        var z = WorldCoordinate.ParseInt(reader);
        return new WorldCoordinates(x, y, z);
    }
}
