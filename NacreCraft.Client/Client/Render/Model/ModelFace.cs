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

namespace NacreCraft.Client.Render.Model;

//ModelFace model face definition, maps to vanilla BlockElementFace
//Records the face's texture reference, cullface direction, UV, and tintindex
//UV defaults to [0,0,16,16], normalized to [0,1] by the Baker and then mapped to atlas UV
//Direction reuses the Gpu layer's Direction enum for consistency
public sealed class ModelFace
{
    //Direction face orientation, reuses Gpu.Direction
    public Direction Direction { get; set; }
    //Texture texture variable reference starting with #, such as #all, resolved to the actual texture path at bake time
    public string Texture { get; set; } = string.Empty;
    //Cullface cullface direction; null means no culling
    //A face can be culled when the neighbor block fully occludes that direction
    public Direction? Cullface { get; set; }
    //UV [u0,v0,u1,v1] in pixel coordinates, 0-16 range, default [0,0,16,16]
    //At bake time divide by 16 to normalize to [0,1], then map to atlas UV via TextureAtlasSprite.MapU/MapV
    public Vector4 UV { get; set; } = new(0, 0, 16, 16);
    //TintIndex tint index; -1 means no tinting, e.g. the side of grass_block uses tintindex=0 to tint green
    //The first version does not support tinting; field is preserved
    public int TintIndex { get; set; } = -1;

    public ModelFace(Direction direction)
    {
        Direction = direction;
    }
}
