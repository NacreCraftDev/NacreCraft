using Silk.NET.Input;

namespace NacreCraft.Client.Blaze3d.Platform.Cursor;

//CursorType one standard cursor shape, maps to vanilla com.mojang.blaze3d.platform.cursor.CursorType
//Vanilla wraps a GLFW cursor handle created once; here the Silk standard cursor enum carries the shape
public sealed class CursorType
{
    public static readonly CursorType Default = new(StandardCursor.Default, "default");

    private readonly StandardCursor _shape;

    internal CursorType(StandardCursor shape, string name)
    {
        _shape = shape;
        Name = name;
    }

    public string Name { get; }

    //Select applies the shape to the window cursor, maps to vanilla select
    //Cursor mode is left alone so a captured in-game cursor keeps its Disabled mode
    public void Select(ICursor cursor) => cursor.StandardCursor = _shape;

    public override string ToString() => Name;
}
