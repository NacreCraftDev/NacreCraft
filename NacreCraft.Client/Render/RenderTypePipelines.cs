using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;

namespace NacreCraft.Client.Render;

//RenderTypePipelines pipelines backing RenderTypes, maps to the scene series of vanilla RenderPipelines
//All of them read the vanilla DynamicTransforms + Projection pair at set0 and declare only the samplers the
//corresponding RenderSetup binds; fog and directional lighting are omitted because NC's shaders bake them
public static class RenderTypePipelines
{
    //SCENE_SNIPPET scene base: matrices set0, opaque color target, depth test+write, quads, cull on
    public static readonly Snippet SCENE_SNIPPET = PipelineBuilder.From()
        .WithBindGroupLayout(BindGroupLayouts.MATRICES_PROJECTION)
        .WithColorTargetState(ColorTargetState.DEFAULT)
        .WithDepthStencilState(DepthStencilState.DEFAULT)
        .WithPrimitiveTopology(PrimitiveTopology.Quads)
        .BuildSnippet();

    private static RenderPipeline Register(PipelineBuilder builder, string location)
        => RenderPipelines.Register(builder.WithLocation(location).Build());

    private static RenderPipeline Register(PipelineBuilder builder)
        => RenderPipelines.Register(builder.Build());

    // ---- block ----

    public static readonly RenderPipeline SOLID_BLOCK = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/block")
        .WithFragmentShader("core/block")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER2)
        .WithVertexBinding(0, DefaultVertexFormat.BLOCK), "pipeline/rt_solid_block");

    public static readonly RenderPipeline CUTOUT_BLOCK = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/block")
        .WithFragmentShader("core/block")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER2)
        .WithVertexBinding(0, DefaultVertexFormat.BLOCK)
        .WithShaderDefine("ALPHA_CUTOUT"), "pipeline/rt_cutout_block");

    public static readonly RenderPipeline TRANSLUCENT_BLOCK = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/block")
        .WithFragmentShader("core/block")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER2)
        .WithVertexBinding(0, DefaultVertexFormat.BLOCK)
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT)), "pipeline/rt_translucent_block");

    public static readonly RenderPipeline CRUMBLING = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_crumbling")
        .WithFragmentShader("core/rendertype_crumbling")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0)
        .WithVertexBinding(0, DefaultVertexFormat.BLOCK)
        .WithCull(false)
        .WithColorTargetState(new ColorTargetState(BlendFunction.FromFactors(BlendFactor.DstColor, BlendFactor.SrcColor, BlendOp.Add)))
        .WithDepthStencilState(new DepthStencilState(CompareOp.GreaterOrEqual, false)), "pipeline/rt_crumbling");

    // ---- beacon / banner ----

    public static readonly RenderPipeline BEACON_BEAM_OPAQUE = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_beacon_beam")
        .WithFragmentShader("core/rendertype_beacon_beam")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0)
        .WithVertexBinding(0, DefaultVertexFormat.BLOCK), "pipeline/rt_beacon_beam_opaque");

    public static readonly RenderPipeline BEACON_BEAM_TRANSLUCENT = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_beacon_beam")
        .WithFragmentShader("core/rendertype_beacon_beam")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0)
        .WithVertexBinding(0, DefaultVertexFormat.BLOCK)
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .WithDepthStencilState(new DepthStencilState(CompareOp.GreaterOrEqual, false)), "pipeline/rt_beacon_beam_translucent");

    public static readonly RenderPipeline BANNER_PATTERN = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_entity")
        .WithFragmentShader("core/rendertype_entity")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER1_SAMPLER2)
        .WithVertexBinding(0, DefaultVertexFormat.ENTITY)
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .WithDepthStencilState(new DepthStencilState(CompareOp.GreaterOrEqual, false)), "pipeline/rt_banner_pattern");

    // ---- entity ----

    public static readonly RenderPipeline ENTITY_SOLID = Register(Entity("pipeline/rt_entity_solid"));
    public static readonly RenderPipeline ENTITY_SOLID_Z_OFFSET_FORWARD = Register(Entity("pipeline/rt_entity_solid_z_offset_forward"));
    public static readonly RenderPipeline ENTITY_CUTOUT_CULL = Register(Entity("pipeline/rt_entity_cutout_cull").WithShaderDefine("ALPHA_CUTOUT"));
    public static readonly RenderPipeline ENTITY_CUTOUT = Register(Entity("pipeline/rt_entity_cutout").WithShaderDefine("ALPHA_CUTOUT").WithCull(false));
    public static readonly RenderPipeline ENTITY_CUTOUT_Z_OFFSET = Register(Entity("pipeline/rt_entity_cutout_z_offset").WithShaderDefine("ALPHA_CUTOUT").WithCull(false));
    public static readonly RenderPipeline ENTITY_CUTOUT_DISSOLVE = Register(Entity("pipeline/rt_entity_cutout_dissolve")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithShaderDefine("DISSOLVE")
        .WithBindGroupLayout(BindGroupLayouts.DISSOLVE_MASK_SAMPLER)
        .WithCull(false));
    public static readonly RenderPipeline ENTITY_TRANSLUCENT = Register(Entity("pipeline/rt_entity_translucent")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .WithCull(false));
    public static readonly RenderPipeline ENTITY_TRANSLUCENT_EMISSIVE = Register(Entity("pipeline/rt_entity_translucent_emissive")
        .WithShaderDefine("EMISSIVE")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .WithDepthStencilState(new DepthStencilState(CompareOp.GreaterOrEqual, false))
        .WithCull(false));
    public static readonly RenderPipeline ENTITY_TRANSLUCENT_CULL = Register(Entity("pipeline/rt_entity_translucent_cull")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT)));
    public static readonly RenderPipeline ARMOR_CUTOUT_NO_CULL = Register(Entity("pipeline/rt_armor_cutout_no_cull").WithShaderDefine("ALPHA_CUTOUT").WithCull(false));
    public static readonly RenderPipeline ARMOR_DECAL_CUTOUT_NO_CULL = Register(Entity("pipeline/rt_armor_decal_cutout_no_cull")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithCull(false)
        .WithDepthStencilState(new DepthStencilState(CompareOp.Equal, false)));
    public static readonly RenderPipeline ARMOR_TRANSLUCENT = Register(Entity("pipeline/rt_armor_translucent")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .WithCull(false));
    public static readonly RenderPipeline END_CRYSTAL_BEAM = Register(Entity("pipeline/rt_end_crystal_beam").WithShaderDefine("ALPHA_CUTOUT").WithCull(false));
    public static readonly RenderPipeline BREEZE_WIND = Register(Entity("pipeline/rt_breeze_wind")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .WithCull(false));
    public static readonly RenderPipeline ENERGY_SWIRL = Register(Entity("pipeline/rt_energy_swirl")
        .WithShaderDefine("EMISSIVE")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithColorTargetState(new ColorTargetState(BlendFunction.ADDITIVE))
        .WithCull(false));
    public static readonly RenderPipeline EYES = Register(Entity("pipeline/rt_eyes")
        .WithShaderDefine("EMISSIVE")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .WithDepthStencilState(new DepthStencilState(CompareOp.GreaterOrEqual, false)));

    public static readonly RenderPipeline ENTITY_SHADOW = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_entity_shadow")
        .WithFragmentShader("core/rendertype_entity_shadow")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER1_SAMPLER2)
        .WithVertexBinding(0, DefaultVertexFormat.ENTITY)
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
        .WithDepthStencilState(new DepthStencilState(CompareOp.GreaterOrEqual, false)), "pipeline/rt_entity_shadow");

    // ---- item ----

    public static readonly RenderPipeline ITEM_CUTOUT = Register(Item("pipeline/rt_item_cutout").WithShaderDefine("ALPHA_CUTOUT"));
    public static readonly RenderPipeline ITEM_TRANSLUCENT = Register(Item("pipeline/rt_item_translucent")
        .WithShaderDefine("ALPHA_CUTOUT")
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT)));

    // ---- outline / glint ----

    public static readonly RenderPipeline OUTLINE_CULL = Register(Outline("pipeline/rt_outline_cull", true));
    public static readonly RenderPipeline OUTLINE_NO_CULL = Register(Outline("pipeline/rt_outline_no_cull", false));

    public static readonly RenderPipeline LEASH = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_leash")
        .WithFragmentShader("core/rendertype_leash")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER2)
        .WithVertexBinding(0, DefaultVertexFormat.POSITION_COLOR_LIGHTMAP)
        .WithCull(false), "pipeline/rt_leash");

    public static readonly RenderPipeline WATER_MASK = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_water_mask")
        .WithFragmentShader("core/rendertype_water_mask")
        .WithVertexBinding(0, DefaultVertexFormat.POSITION)
        .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT)), "pipeline/rt_water_mask");

    public static readonly RenderPipeline GLINT = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/glint")
        .WithFragmentShader("core/glint")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0)
        .WithVertexBinding(0, DefaultVertexFormat.POSITION_TEX)
        .WithCull(false)
        .WithColorTargetState(new ColorTargetState(BlendFunction.GLINT))
        .WithDepthStencilState(new DepthStencilState(CompareOp.Equal, false)), "pipeline/rt_glint");

    // ---- text ----

    public static readonly RenderPipeline TEXT = Register(Text("pipeline/rt_text"));
    public static readonly RenderPipeline TEXT_GRAYSCALE = Register(Text("pipeline/rt_text_grayscale").WithShaderDefine("IS_GRAYSCALE"));
    public static readonly RenderPipeline TEXT_SEE_THROUGH = Register(Text("pipeline/rt_text_see_through")
        .WithShaderDefine("IS_SEE_THROUGH")
        .WithDepthStencilState((DepthStencilState?)null));
    public static readonly RenderPipeline TEXT_GRAYSCALE_SEE_THROUGH = Register(Text("pipeline/rt_text_grayscale_see_through")
        .WithShaderDefine("IS_GRAYSCALE")
        .WithShaderDefine("IS_SEE_THROUGH")
        .WithDepthStencilState((DepthStencilState?)null));
    public static readonly RenderPipeline TEXT_POLYGON_OFFSET = Register(Text("pipeline/rt_text_polygon_offset")
        .WithDepthStencilState(new DepthStencilState(CompareOp.GreaterOrEqual, true, 1.0f, 10.0f)));
    public static readonly RenderPipeline TEXT_GRAYSCALE_POLYGON_OFFSET = Register(Text("pipeline/rt_text_grayscale_polygon_offset")
        .WithShaderDefine("IS_GRAYSCALE")
        .WithDepthStencilState(new DepthStencilState(CompareOp.GreaterOrEqual, true, 1.0f, 10.0f)));

    public static readonly RenderPipeline TEXT_BACKGROUND = Register(TextBackground("pipeline/rt_text_background", true));
    public static readonly RenderPipeline TEXT_BACKGROUND_SEE_THROUGH = Register(TextBackground("pipeline/rt_text_background_see_through", false));

    // ---- weather / portal ----

    public static readonly RenderPipeline LIGHTNING = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_lightning")
        .WithFragmentShader("core/rendertype_lightning")
        .WithVertexBinding(0, DefaultVertexFormat.POSITION_COLOR)
        .WithCull(false)
        .WithColorTargetState(new ColorTargetState(BlendFunction.LIGHTNING)), "pipeline/rt_lightning");

    public static readonly RenderPipeline DRAGON_RAYS = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_lightning")
        .WithFragmentShader("core/rendertype_lightning")
        .WithVertexBinding(0, DefaultVertexFormat.POSITION_COLOR)
        .WithCull(false)
        .WithColorTargetState(new ColorTargetState(BlendFunction.LIGHTNING))
        .WithPrimitiveTopology(PrimitiveTopology.TriangleList), "pipeline/rt_dragon_rays");

    public static readonly RenderPipeline END_PORTAL = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_end_portal")
        .WithFragmentShader("core/rendertype_end_portal")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER1)
        .WithVertexBinding(0, DefaultVertexFormat.POSITION), "pipeline/rt_end_portal");

    public static readonly RenderPipeline END_GATEWAY = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/rendertype_end_portal")
        .WithFragmentShader("core/rendertype_end_portal")
        .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER1)
        .WithVertexBinding(0, DefaultVertexFormat.POSITION), "pipeline/rt_end_gateway");

    // ---- lines / debug ----

    public static readonly RenderPipeline LINES = Register(Lines("pipeline/rt_lines", true));
    public static readonly RenderPipeline LINES_TRANSLUCENT = Register(Lines("pipeline/rt_lines_translucent", false));
    public static readonly RenderPipeline SECONDARY_BLOCK_OUTLINE = Register(Lines("pipeline/rt_secondary_block_outline", false));

    public static readonly RenderPipeline DEBUG_FILLED_BOX = Register(DebugQuads("pipeline/rt_debug_filled_box", true));
    public static readonly RenderPipeline DEBUG_QUADS = Register(DebugQuads("pipeline/rt_debug_quads", false));
    public static readonly RenderPipeline DEBUG_TRIANGLE_FAN = Register(DebugQuads("pipeline/rt_debug_triangle_fan", false)
        .WithPrimitiveTopology(PrimitiveTopology.TriangleFan));

    public static readonly RenderPipeline DEBUG_POINTS = Register(PipelineBuilder.From(SCENE_SNIPPET)
        .WithVertexShader("core/debug_point")
        .WithFragmentShader("core/debug_point")
        .WithVertexBinding(0, DefaultVertexFormat.POSITION_COLOR_LINE_WIDTH)
        .WithPrimitiveTopology(PrimitiveTopology.Points)
        .WithCull(false), "pipeline/rt_debug_points");

    public static readonly RenderPipeline BLOCK_SCREEN_EFFECT = Register(ScreenEffect("pipeline/rt_block_screen_effect"));
    public static readonly RenderPipeline FIRE_SCREEN_EFFECT = Register(ScreenEffect("pipeline/rt_fire_screen_effect"));

    // ---- helpers ----

    private static PipelineBuilder Entity(string location)
        => PipelineBuilder.From(SCENE_SNIPPET)
            .WithLocation(location)
            .WithVertexShader("core/rendertype_entity")
            .WithFragmentShader("core/rendertype_entity")
            .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER1_SAMPLER2)
            .WithVertexBinding(0, DefaultVertexFormat.ENTITY);

    private static PipelineBuilder Item(string location)
        => PipelineBuilder.From(SCENE_SNIPPET)
            .WithLocation(location)
            .WithVertexShader("core/rendertype_item")
            .WithFragmentShader("core/rendertype_item")
            .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER1_SAMPLER2)
            .WithVertexBinding(0, DefaultVertexFormat.ENTITY);

    private static PipelineBuilder Outline(string location, bool cull)
        => PipelineBuilder.From(SCENE_SNIPPET)
            .WithLocation(location)
            .WithVertexShader("core/rendertype_outline")
            .WithFragmentShader("core/rendertype_outline")
            .WithBindGroupLayout(BindGroupLayouts.SAMPLER0)
            .WithVertexBinding(0, DefaultVertexFormat.POSITION_TEX_COLOR)
            .WithCull(cull)
            .WithDepthStencilState((DepthStencilState?)null);

    private static PipelineBuilder Text(string location)
        => PipelineBuilder.From(SCENE_SNIPPET)
            .WithLocation(location)
            .WithVertexShader("core/rendertype_text")
            .WithFragmentShader("core/rendertype_text")
            .WithBindGroupLayout(BindGroupLayouts.SAMPLER0_SAMPLER2)
            .WithVertexBinding(0, DefaultVertexFormat.POSITION_TEX_COLOR)
            .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT));

    private static PipelineBuilder TextBackground(string location, bool depth)
        => PipelineBuilder.From(SCENE_SNIPPET)
            .WithLocation(location)
            .WithVertexShader("core/rendertype_text_background")
            .WithFragmentShader("core/rendertype_text_background")
            .WithBindGroupLayout(BindGroupLayouts.SAMPLER2)
            .WithVertexBinding(0, DefaultVertexFormat.POSITION_COLOR_LIGHTMAP)
            .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
            .WithDepthStencilState(depth ? DepthStencilState.DEFAULT : null);

    private static PipelineBuilder Lines(string location, bool depthWrite)
        => PipelineBuilder.From(SCENE_SNIPPET)
            .WithLocation(location)
            .WithVertexShader("core/rendertype_lines")
            .WithFragmentShader("core/rendertype_lines")
            .WithVertexBinding(0, DefaultVertexFormat.POSITION_COLOR_NORMAL_LINE_WIDTH)
            .WithPrimitiveTopology(PrimitiveTopology.Lines)
            .WithCull(false)
            .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
            .WithDepthStencilState(depthWrite ? DepthStencilState.DEFAULT : new DepthStencilState(CompareOp.GreaterOrEqual, false));

    private static PipelineBuilder DebugQuads(string location, bool depthWrite)
        => PipelineBuilder.From(SCENE_SNIPPET)
            .WithLocation(location)
            .WithVertexShader("core/rendertype_lightning")
            .WithFragmentShader("core/rendertype_lightning")
            .WithVertexBinding(0, DefaultVertexFormat.POSITION_COLOR)
            .WithCull(false)
            .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
            .WithDepthStencilState(depthWrite ? new DepthStencilState(CompareOp.GreaterOrEqual, false) : new DepthStencilState(CompareOp.GreaterOrEqual, false));

    private static PipelineBuilder ScreenEffect(string location)
        => PipelineBuilder.From(SCENE_SNIPPET)
            .WithLocation(location)
            .WithVertexShader("core/rendertype_screen_effect")
            .WithFragmentShader("core/rendertype_screen_effect")
            .WithBindGroupLayout(BindGroupLayouts.SAMPLER0)
            .WithVertexBinding(0, DefaultVertexFormat.POSITION_TEX_COLOR)
            .WithColorTargetState(new ColorTargetState(BlendFunction.TRANSLUCENT))
            .WithDepthStencilState((DepthStencilState?)null);
}
