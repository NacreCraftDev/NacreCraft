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
using NacreCraft.Client.Blaze3d;
namespace NacreCraft.Client.Blaze3d.Pipeline;

//ColorTargetState color attachment state, maps to vanilla ColorTargetState record
//Describes the format/blend/write mask of the pipeline output attachment
//blendFunction=null means blending is disabled; writeMask defaults to WRITE_ALL
public readonly record struct ColorTargetState(BlendFunction? BlendFunction, GpuFormat Format, int WriteMask)
{
    public const int WriteRed = 1;
    public const int WriteGreen = 2;
    public const int WriteBlue = 4;
    public const int WriteAlpha = 8;
    public const int WriteColor = WriteRed | WriteGreen | WriteBlue;
    public const int WriteAll = WriteRed | WriteGreen | WriteBlue | WriteAlpha;
    public const int WriteNone = 0;
    public const int MaxColorTargets = 8;

    //DEFAULT default state RGBA8_UNORM no blending all-channel write
    public static readonly ColorTargetState DEFAULT = new(null, GpuFormat.Rgba8Unorm, WriteAll);

    public ColorTargetState(BlendFunction blendFunction)
        : this(blendFunction, GpuFormat.Rgba8Unorm, WriteAll) { }

    public bool RedChannel => (WriteMask & WriteRed) != 0;
    public bool GreenChannel => (WriteMask & WriteGreen) != 0;
    public bool BlueChannel => (WriteMask & WriteBlue) != 0;
    public bool AlphaChannel => (WriteMask & WriteAlpha) != 0;
}
