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

//CubeModel procedural cube model, maps to vanilla ModelPart.Cube.compile
//Generates 6 face BakedQuads for ItemRenderer to render; the PoC uses procedural geometry instead of loading a JSON model
//Each face has 4 vertices + UV + face normal (Direction); vertices are counter-clockwise with the front facing out
public static class CubeModel
{
    //Create generates a BakedQuad list for a cube of side size centered at the origin
    //UVs map to 0..1 per face; the PoC does no texture atlas chunking
    public static List<BakedQuad> Create(float size)
    {
        var s = size / 2f;
        var quads = new List<BakedQuad>(6);
        //Down -Y
        quads.Add(new BakedQuad(
            new Vector3(-s, -s, s), new Vector3(s, -s, s),
            new Vector3(s, -s, -s), new Vector3(-s, -s, -s),
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1), Direction.Down));
        //Up +Y
        quads.Add(new BakedQuad(
            new Vector3(-s, s, -s), new Vector3(s, s, -s),
            new Vector3(s, s, s), new Vector3(-s, s, s),
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1), Direction.Up));
        //North -Z
        quads.Add(new BakedQuad(
            new Vector3(-s, -s, -s), new Vector3(s, -s, -s),
            new Vector3(s, s, -s), new Vector3(-s, s, -s),
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1), Direction.North));
        //South +Z
        quads.Add(new BakedQuad(
            new Vector3(s, -s, s), new Vector3(-s, -s, s),
            new Vector3(-s, s, s), new Vector3(s, s, s),
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1), Direction.South));
        //West -X
        quads.Add(new BakedQuad(
            new Vector3(-s, -s, -s), new Vector3(-s, -s, s),
            new Vector3(-s, s, s), new Vector3(-s, s, -s),
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1), Direction.West));
        //East +X
        quads.Add(new BakedQuad(
            new Vector3(s, -s, s), new Vector3(s, -s, -s),
            new Vector3(s, s, -s), new Vector3(s, s, s),
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1), Direction.East));
        return quads;
    }
}
