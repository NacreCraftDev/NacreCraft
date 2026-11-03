using Silk.NET.Input;

namespace NacreCraft.Client.Blaze3d.Platform.Cursor;

//CursorTypes the standard cursor shapes vanilla declares, maps to com.mojang.blaze3d.platform.cursor.CursorTypes
public static class CursorTypes
{
    public static readonly CursorType Arrow = new(StandardCursor.Arrow, "arrow");
    public static readonly CursorType IBeam = new(StandardCursor.IBeam, "ibeam");
    public static readonly CursorType Crosshair = new(StandardCursor.Crosshair, "crosshair");
    public static readonly CursorType PointingHand = new(StandardCursor.Hand, "pointing_hand");
    public static readonly CursorType ResizeNs = new(StandardCursor.VResize, "resize_ns");
    public static readonly CursorType ResizeEw = new(StandardCursor.HResize, "resize_ew");
    public static readonly CursorType ResizeAll = new(StandardCursor.ResizeAll, "resize_all");
    public static readonly CursorType NotAllowed = new(StandardCursor.NotAllowed, "not_allowed");
}
