using NacreCraft.Client.Level;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Primitives;
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

namespace NacreCraft.Client.Render.World;

//ChunkLightSampler chunk light sampler, maps to vanilla SectionRenderBuilder's light computation
//Fetches the block/sky light of a block face's outer neighbor from ClientLevel, merges emission, and packs it
//LightCoords encoding (blockLight<<4)|(skyLight<<20), aligning with LightTexture.PackLightCoords
//Emission merge takes max(unpackedBlockLight, emission) without touching the sky segment
public sealed class ChunkLightSampler
{
    private readonly ClientLevel _level;

    public ChunkLightSampler(ClientLevel level) => _level = level;

    //GetLightCoords gets the packed light coords at the face's outer neighbor position
    //neighborX/Y/Z are the world coordinates of the face's outer neighbor; lightEmission is the block's own emission level 0-15
    //For out-of-bounds neighbors, ClientLevel returns block=0/sky=15, aligning with vanilla default behavior
    public int GetLightCoords(int neighborX, int neighborY, int neighborZ, int lightEmission)
    {
        var neighborPos = new BlockPos(neighborX, neighborY, neighborZ);
        var blockLight = _level.GetBlockLight(neighborPos);
        var skyLight = _level.GetSkyLight(neighborPos);
        //A glowing block's own light raises the blockLight segment without touching the sky segment
        if (lightEmission > blockLight) blockLight = lightEmission;
        return LightTexture.PackLightCoords(blockLight, skyLight);
    }
}
