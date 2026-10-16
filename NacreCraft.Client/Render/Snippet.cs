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

//Snippet pipeline snippet, maps to vanilla RenderPipeline.Snippet record
//All fields are optional for composition; later non-null fields override earlier ones, supporting snippet composition in RenderPipelines
public sealed class Snippet
{
    public string? VertexShader { get; }
    public string? FragmentShader { get; }
    public ShaderDefines? ShaderDefines { get; }
    public IReadOnlyList<BindGroupLayout>? BindGroupLayouts { get; }
    public IReadOnlyList<ColorTargetState?> ColorTargetStates { get; }
    public DepthStencilState? DepthStencilState { get; }
    //OverrideDepthStencilState whether DepthStencilState was set explicitly on this snippet
    //Without the flag a null state cannot be told apart from "this snippet says nothing about depth", which is what
    //lets a snippet clear the depth state it inherited, the way GUI_TEXT_SNIPPET drops TEXT_SNIPPET's default
    public bool OverrideDepthStencilState { get; }
    public PolygonMode? PolygonMode { get; }
    public bool? Cull { get; }
    public IReadOnlyList<VertexFormat?> VertexFormatPerBuffer { get; }
    public PrimitiveTopology? PrimitiveTopology { get; }
    //PushConstantSize size in bytes of the push constant block; null leaves the current value untouched
    public int? PushConstantSize { get; }

    public Snippet(
        string? vertexShader,
        string? fragmentShader,
        ShaderDefines? shaderDefines,
        IReadOnlyList<BindGroupLayout>? bindGroupLayouts,
        IReadOnlyList<ColorTargetState?> colorTargetStates,
        DepthStencilState? depthStencilState,
        PolygonMode? polygonMode,
        bool? cull,
        IReadOnlyList<VertexFormat?> vertexFormatPerBuffer,
        PrimitiveTopology? primitiveTopology,
        int? pushConstantSize = null,
        bool overrideDepthStencilState = false)
    {
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
        OverrideDepthStencilState = overrideDepthStencilState;
    }
}
