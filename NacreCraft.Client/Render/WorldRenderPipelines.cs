using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Render;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.World;
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
namespace NacreCraft.Client.Render;

//WorldRenderPipelines world render pipeline registry, maps to the terrain series of vanilla RenderPipelines
//The 3 terrain pipelines share TERRAIN_SNIPPET, differing only in blend/define
//set 0 MATRICES_PROJECTION with a ViewProj mat4; a 16-byte push constant carries the region origin, so vertices stay section-local
//set 1 SAMPLER0_SAMPLER1 atlas texture + lightmap
//Vertex format is the 20-byte compact chunk format; face shading is baked into the vertex color, so the Lighting UBO is not needed
public static class WorldRenderPipelines
{
    //PushConstantSize bytes pushed before each terrain run: a vec4 carrying the region origin
    //The section slot baked into every vertex supplies the in-region offset, so one push covers a whole region
    public const int TerrainPushConstantSize = 16;

    //TERRAIN_SNIPPET terrain base snippet shared by all terrain pipelines
    //Vertex format is ChunkVertexFormat, same sprite/light packing for the whole mesh, Quads topology, depth GEQUAL with write
    public static readonly Snippet TERRAIN_SNIPPET = PipelineBuilder.From()
        .WithVertexShader("core/terrain")
        .WithFragmentShader("core/terrain")
        .WithBindGroupLayout(BindGroupLayouts.MATRICES_PROJECTION)
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER1)
        .WithColorTargetState(ColorTargetState.DEFAULT)
        .WithDepthStencilState(DepthStencilState.DEFAULT)
        .WithVertexBinding(0, ChunkVertexFormat.Format)
        .WithPrimitiveTopology(PrimitiveTopology.Quads)
        .WithPushConstantSize(TerrainPushConstantSize)
        .BuildSnippet();

    //SOLID_TERRAIN opaque terrain pipeline, no blend, no alpha cutout
    //Used for fully opaque blocks like stone and dirt; all 6 faces render with depth write
    public static readonly RenderPipeline SOLID_TERRAIN = RenderPipelines.Register(PipelineBuilder.From(TERRAIN_SNIPPET)
        .WithLocation("pipeline/solid_terrain")
        .Build());

    //CUTOUT_TERRAIN alpha-cutout terrain pipeline, no blend, shader discards alpha<0.5
    //Used for cutout textures like leaves and plants; no blend, transparent pixels discarded to keep depth correct
    public static readonly RenderPipeline CUTOUT_TERRAIN = RenderPipelines.Register(PipelineBuilder.From(TERRAIN_SNIPPET)
        .WithLocation("pipeline/cutout_terrain")
        .WithShaderDefine("ALPHA_CUTOUT")
        .Build());

    //TRANSLUCENT_TERRAIN translucent terrain pipeline TRANSLUCENT blend with no depth write to avoid occlusion
    //Used for translucent blocks like water, ice and glass; rendered after Solid/Cutout in RenderLayer order
    public static readonly RenderPipeline TRANSLUCENT_TERRAIN = RenderPipelines.Register(PipelineBuilder.From(TERRAIN_SNIPPET)
        .WithLocation("pipeline/translucent_terrain")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .Build());
}
