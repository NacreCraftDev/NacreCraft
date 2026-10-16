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

namespace NacreCraft.Client.Gui.Render.Pip;

//PictureInPictureRenderState PIP render state interface, maps to vanilla pip.PictureInPictureRenderState
//Render state value object for 3D content embedded in the GUI (player skins/banners/entity previews)
//Built and submitted by Screen during the submission phase into GuiRenderState.AddPictureInPicture
//During the render phase the PictureInPictureRenderer<T> subclass renders 3D content offscreen then blits it to the GUI
public interface PictureInPictureRenderState
{
    //X0/Y0/X1/Y1 the PIP area rectangle in GUI coordinates, top-left/bottom-right, maps to vanilla x0/y0/x1/y1
    int X0 { get; }
    int Y0 { get; }
    int X1 { get; }
    int Y1 { get; }

    //Scale 3D content scale factor, maps to vanilla scale, set by the subclass per model size
    float Scale { get; }

    //ScissorArea scissor rectangle; empty means no clipping
    ScreenRectangle ScissorArea { get; }

    //Pose 2D transform matrix, identity by default, maps to vanilla pose() default IDENTITY_POSE
    //The PIP area usually has no pose transform; blit uses X0/Y0/X1/Y1 directly
    Matrix3x2 Pose { get; }

    //Bounds used for level intersection tests, derived from intersecting the geometry rectangle with the scissor
    ScreenRectangle Bounds { get; }

    //GetBounds computes the bounds by intersecting the geometry rectangle with the scissor, maps to vanilla getBounds
    //Uses the geometry rectangle directly when the scissor is empty, otherwise the intersection
    public static ScreenRectangle GetBounds(int x0, int y0, int x1, int y1, ScreenRectangle scissorArea)
    {
        var raw = new ScreenRectangle(x0, y0, x1 - x0, y1 - y0);
        return scissorArea.IsEmpty ? raw : scissorArea.Intersect(raw) ?? raw;
    }
}
