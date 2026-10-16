using System.Numerics;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;

namespace NacreCraft.Client.Gui.Navigation;

//ScreenRectangle screen rectangle, maps to vanilla ScreenRectangle
//Used for RenderState's ScissorArea and Bounds level intersection tests
public readonly record struct ScreenRectangle(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;

    //Intersects whether two rectangles intersect, maps to vanilla intersects
    public bool Intersects(ScreenRectangle other)
        => other.Left < Right && other.Right > Left && other.Top < Bottom && other.Bottom > Top;

    //Encompasses whether this rectangle fully contains other, maps to vanilla encompasses
    public bool Encompasses(ScreenRectangle other)
        => other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom;

    //Contains an alias of Encompasses kept for legacy callers
    public bool Contains(ScreenRectangle other) => Encompasses(other);

    //Intersect computes the intersection and returns null when disjoint, maps to vanilla intersection
    public ScreenRectangle? Intersect(ScreenRectangle other)
    {
        var left = Math.Max(Left, other.Left);
        var top = Math.Max(Top, other.Top);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        if (left >= right || top >= bottom) return null;
        return new ScreenRectangle(left, top, right - left, bottom - top);
    }

    //TransformMaxBounds transforms the four corners with the pose and takes the max bounding box, maps to vanilla transformMaxBounds
    //Used to derive Bounds from the geometry rectangle + pose when constructing a RenderState
    public ScreenRectangle TransformMaxBounds(Matrix3x2 pose)
    {
        var topLeft = Vector2.Transform(new Vector2(Left, Top), pose);
        var topRight = Vector2.Transform(new Vector2(Right, Top), pose);
        var bottomLeft = Vector2.Transform(new Vector2(Left, Bottom), pose);
        var bottomRight = Vector2.Transform(new Vector2(Right, Bottom), pose);
        var minX = Math.Min(Math.Min(topLeft.X, bottomLeft.X), Math.Min(topRight.X, bottomRight.X));
        var maxX = Math.Max(Math.Max(topLeft.X, bottomLeft.X), Math.Max(topRight.X, bottomRight.X));
        var minY = Math.Min(Math.Min(topLeft.Y, bottomLeft.Y), Math.Min(topRight.Y, bottomRight.Y));
        var maxY = Math.Max(Math.Max(topLeft.Y, bottomLeft.Y), Math.Max(topRight.Y, bottomRight.Y));
        return new ScreenRectangle(
            (int)Math.Floor(minX),
            (int)Math.Floor(minY),
            (int)Math.Ceiling(maxX - minX),
            (int)Math.Ceiling(maxY - minY));
    }

    public static readonly ScreenRectangle Empty = default;

    //IsEmpty a non-positive width or height means an empty rectangle
    public bool IsEmpty => Width <= 0 || Height <= 0;
}
