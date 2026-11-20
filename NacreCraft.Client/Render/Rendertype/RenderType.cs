using System.Numerics;
using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Registry;
using Topology = NacreCraft.Client.Blaze3d.Pipeline.PrimitiveTopology;

namespace NacreCraft.Client.Render.Rendertype;

//RenderType a named drawing state for batched geometry, maps to vanilla net.minecraft.client.renderer.rendertype.RenderType
//Geometry is grouped by RenderType so one pipeline/texture bind covers many draws; Prepare resolves the per-draw GPU state
public sealed class RenderType
{
    public const int MegaByte = 0x100000;
    public const int BigBufferSize = 0x400000;
    public const int SmallBufferSize = 786432;
    public const int TransientBufferSize = 1536;

    private static Func<Identifier, bool, RenderType>? _outlineFactory;

    private readonly RenderSetup _state;
    private readonly string _name;
    private RenderType? _outline;
    private bool _outlineResolved;

    private RenderType(string name, RenderSetup state)
    {
        _name = name;
        _state = state;
    }

    //Create builds a render type; the outline factory is installed by RenderTypes to avoid a static cycle
    internal static RenderType Create(string name, RenderSetup state) => new(name, state);

    internal static void SetOutlineFactory(Func<Identifier, bool, RenderType> factory) => _outlineFactory = factory;

    public override string ToString() => $"RenderType[{_name}:{_state}]";

    public bool HasBlending() => _state.Pipeline.GetColorTargetState().BlendFunction is not null;

    public OutputTarget OutputTarget() => _state.OutputTarget;

    //CurrentContext supplies the texture manager and the overlay/lightmap views Prepare needs; the game renderer installs it
    public static RenderTypeContext? CurrentContext { get; set; }

    public PreparedRenderType Prepare()
    {
        var context = CurrentContext
                      ?? throw new InvalidOperationException("RenderType.Prepare requires RenderType.CurrentContext to be installed");
        var textures = _state.PrepareTextures(context.TextureManager, RenderSystem.GetSamplerCache(),
            context.OverlayTexture, context.LightmapTexture);
        return new PreparedRenderType(_state.Pipeline, _state.OutputTarget,
            WriteDynamicTransforms(RenderSystem.GetModelViewMatrixCopy()),
            new ScissorState(RenderSystem.GetScissorStateForRenderTypeDraws()), textures);
    }

    private GpuBufferSlice WriteDynamicTransforms(Matrix4x4 modelViewMatrix)
    {
        var modifier = _state.LayeringTransform.GetModifier();
        if (modifier is not null) modelViewMatrix = modifier(modelViewMatrix);
        return RenderSystem.GetDynamicUniforms().WriteTransform(modelViewMatrix, _state.TextureTransform.CreateMatrix());
    }

    public VertexFormat Format()
        => _state.Pipeline.GetVertexFormatBinding(0)
           ?? throw new InvalidOperationException($"Render type {_name} has no vertex format binding");

    public PrimitiveTopology PrimitiveTopology() => _state.Pipeline.PrimitiveTopology;

    public RenderType? Outline()
    {
        if (!_outlineResolved)
        {
            _outline = _state.Outline == RenderSetup.OutlineProperty.AffectsOutline && _outlineFactory is not null
                       && _state.Textures.Count > 0
                ? _outlineFactory(_state.Textures.Values.First().Location, _state.Pipeline.Cull)
                : null;
            _outlineResolved = true;
        }

        return _outline;
    }

    public bool IsOutline() => _state.Outline == RenderSetup.OutlineProperty.IsOutline;

    public RenderPipeline Pipeline() => _state.Pipeline;

    public bool AffectsCrumbling() => _state.AffectsCrumbling;

    public bool CanConsolidateConsecutiveGeometry()
        => PrimitiveTopology() is not (Topology.LineStrip or Topology.TriangleStrip or Topology.TriangleFan);

    public bool SortOnUpload() => _state.SortOnUpload;

    //RenderTypeContext the runtime textures Prepare needs, a NacreCraft stand-in for Minecraft.gameRenderer
    public sealed record RenderTypeContext(TextureManager TextureManager, GpuTextureView? OverlayTexture, GpuTextureView? LightmapTexture);
}
