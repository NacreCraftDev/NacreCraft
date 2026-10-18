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
namespace NacreCraft.Client.Resources.Metadata.Gui;

//GuiSprite one sprite of the GUI atlas, maps to vanilla TextureAtlasSprite in AtlasIds.GUI
//The region is expressed in atlas pixels: AtlasX/AtlasY is the top-left corner of this sprite inside the atlas, and
//AtlasWidth/AtlasHeight describe the whole atlas. Width/Height are the sprite's own pixel size
//Scaling is the parsed .mcmeta gui.scaling result
//TextureId is only set by the legacy standalone-texture path, where the sprite covers its whole texture
public sealed class GuiSprite
{
    public int TextureId { get; }
    public TextureSetup Texture { get; }
    public int Width { get; }
    public int Height { get; }
    public GuiSpriteScaling Scaling { get; }
    public int AtlasX { get; }
    public int AtlasY { get; }
    public int AtlasWidth { get; }
    public int AtlasHeight { get; }

    //Atlas sprite constructor, the UVs come from the region inside the shared GUI atlas
    public GuiSprite(int atlasX, int atlasY, int atlasWidth, int atlasHeight,
        int width, int height, TextureSetup texture, GuiSpriteScaling scaling)
    {
        TextureId = 0;
        Texture = texture;
        Width = width;
        Height = height;
        Scaling = scaling;
        AtlasX = atlasX;
        AtlasY = atlasY;
        AtlasWidth = atlasWidth;
        AtlasHeight = atlasHeight;
    }

    //Standalone texture constructor for the legacy per-file path; the sprite covers the whole texture
    public GuiSprite(int textureId, TextureSetup texture, int width, int height, GuiSpriteScaling scaling)
    {
        TextureId = textureId;
        Texture = texture;
        Width = width;
        Height = height;
        Scaling = scaling;
        AtlasX = 0;
        AtlasY = 0;
        AtlasWidth = width;
        AtlasHeight = height;
    }
}
