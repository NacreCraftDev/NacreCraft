using System.Threading;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Render;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
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

namespace NacreCraft.Client.Blaze3d.Pipeline;

//RenderPipeline declarative render pipeline, maps to vanilla RenderPipeline record
//Immutable value object carrying declaration fields location/shaders/defines/bindGroupLayouts/depth/colorTargets/topology
//Produced by PipelineBuilder.Build; sortKey increases monotonically to drive render sorting and batching
//Compiled via IGpuDevice.PrecompilePipeline into a CompiledRenderPipeline holding a VkPipeline handle
public sealed class RenderPipeline
{
    private static int s_nextSortKey;

    public string Location { get; }
    public string VertexShader { get; }
    public string FragmentShader { get; }
    public ShaderDefines ShaderDefines { get; }
    public IReadOnlyList<BindGroupLayout> BindGroupLayouts { get; }
    public DepthStencilState? DepthStencilState { get; }
    public PolygonMode PolygonMode { get; }
    public bool Cull { get; }
    public IReadOnlyList<ColorTargetState> ColorTargetStates { get; }
    public IReadOnlyList<VertexFormat?> VertexFormatPerBuffer { get; }
    public PrimitiveTopology PrimitiveTopology { get; }
    public int SortKey { get; }
    //PushConstantSize size in bytes of the push constant block; 0 means the pipeline has none
    public int PushConstantSize { get; }

    internal RenderPipeline(
        string location,
        string vertexShader,
        string fragmentShader,
        ShaderDefines shaderDefines,
        IReadOnlyList<BindGroupLayout> bindGroupLayouts,
        IReadOnlyList<ColorTargetState> colorTargetStates,
        DepthStencilState? depthStencilState,
        PolygonMode polygonMode,
        bool cull,
        IReadOnlyList<VertexFormat?> vertexFormatPerBuffer,
        PrimitiveTopology primitiveTopology,
        int pushConstantSize)
    {
        Location = location;
        VertexShader = vertexShader;
        FragmentShader = fragmentShader;
        ShaderDefines = shaderDefines;
        BindGroupLayouts = bindGroupLayouts;
        ColorTargetStates = colorTargetStates;
        DepthStencilState = depthStencilState;
        PolygonMode = polygonMode;
        Cull = cull;
        VertexFormatPerBuffer = vertexFormatPerBuffer;
        PrimitiveTopology = primitiveTopology;
        PushConstantSize = pushConstantSize;
        SortKey = Interlocked.Increment(ref s_nextSortKey) - 1;
    }

    //WantsDepthTexture whether a depth texture attachment is needed, depthStencilState != null
    public bool WantsDepthTexture() => DepthStencilState != null;

    public ColorTargetState GetColorTargetState() => ColorTargetStates[0];

    public VertexFormat? GetVertexFormatBinding(int bindingIndex) => VertexFormatPerBuffer[bindingIndex];

    public override string ToString() => Location;
}
