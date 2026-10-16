using System.Collections.ObjectModel;
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

//PipelineBuilder pipeline builder, maps to vanilla RenderPipeline.Builder
//Supports fluent WithXxx chained declarations + From(snippets) snippet composition
//BuildSnippet produces a reusable Snippet; Build produces a full RenderPipeline including sortKey
public sealed class PipelineBuilder
{
    private const int MaxColorTargets = ColorTargetState.MaxColorTargets;
    private const int MaxVertexBindings = 16;
    private const int MaxVertexAttributes = 16;

    private string? _location;
    private string? _vertexShader;
    private string? _fragmentShader;
    private ShaderDefines.Builder? _definesBuilder;
    private List<BindGroupLayout>? _bindGroupLayouts;
    private DepthStencilState? _depthStencilState;
    //_depthStencilStateSet whether the depth state was set explicitly, so a null value can clear an inherited one
    private bool _depthStencilStateSet;
    private PolygonMode? _polygonMode;
    private bool? _cull;
    private readonly ColorTargetState?[] _colorTargetStates = new ColorTargetState?[MaxColorTargets];
    private int _activeColorTargetStateCount;
    private readonly VertexFormat?[] _vertexFormatPerBuffer = new VertexFormat?[MaxVertexBindings];
    private PrimitiveTopology? _primitiveTopology;
    private int? _pushConstantSize;

    public static PipelineBuilder From(params Snippet[] snippets)
    {
        var builder = new PipelineBuilder();
        foreach (var s in snippets)
            builder.WithSnippet(s);
        return builder;
    }

    public PipelineBuilder WithLocation(string location) { _location = location; return this; }

    public PipelineBuilder WithVertexShader(string vertexShader) { _vertexShader = vertexShader; return this; }

    public PipelineBuilder WithFragmentShader(string fragmentShader) { _fragmentShader = fragmentShader; return this; }

    public PipelineBuilder WithShaderDefine(string key)
    {
        _definesBuilder ??= ShaderDefines.NewBuilder();
        _definesBuilder.Define(key);
        return this;
    }

    public PipelineBuilder WithShaderDefine(string key, int value)
    {
        _definesBuilder ??= ShaderDefines.NewBuilder();
        _definesBuilder.Define(key, value);
        return this;
    }

    public PipelineBuilder WithShaderDefine(string key, float value)
    {
        _definesBuilder ??= ShaderDefines.NewBuilder();
        _definesBuilder.Define(key, value);
        return this;
    }

    public PipelineBuilder WithBindGroupLayout(BindGroupLayout layout)
    {
        _bindGroupLayouts ??= new List<BindGroupLayout>();
        _bindGroupLayouts.Add(layout);
        return this;
    }

    public PipelineBuilder WithPolygonMode(PolygonMode mode) { _polygonMode = mode; return this; }

    public PipelineBuilder WithCull(bool cull) { _cull = cull; return this; }

    public PipelineBuilder WithColorTargetState(ColorTargetState state) => WithColorTargetState(0, state);

    public PipelineBuilder WithColorTargetState(int index, ColorTargetState state)
    {
        _colorTargetStates[index] = state;
        _activeColorTargetStateCount = Math.Max(_activeColorTargetStateCount, index + 1);
        return this;
    }

    public PipelineBuilder WithUnusedColorTargetState(int index)
    {
        _colorTargetStates[index] = null;
        _activeColorTargetStateCount = Math.Max(_activeColorTargetStateCount, index + 1);
        return this;
    }

    public PipelineBuilder WithDepthStencilState(DepthStencilState state)
        => WithDepthStencilState((DepthStencilState?)state);

    //WithDepthStencilState(null) clears the depth state so the pipeline neither tests nor writes depth, which is what GUI needs
    public PipelineBuilder WithDepthStencilState(DepthStencilState? state)
    {
        _depthStencilState = state;
        _depthStencilStateSet = true;
        return this;
    }

    public PipelineBuilder WithVertexBinding(int bindingIndex, VertexFormat format)
    {
        _vertexFormatPerBuffer[bindingIndex] = format;
        return this;
    }

    public PipelineBuilder WithPrimitiveTopology(PrimitiveTopology topology)
    {
        _primitiveTopology = topology;
        return this;
    }

    //WithPushConstantSize declares a push constant block carried by the pipeline layout
    public PipelineBuilder WithPushConstantSize(int size)
    {
        _pushConstantSize = size;
        return this;
    }

    //WithSnippet merges a snippet; later non-null fields override earlier ones, shaderDefines merge and bindGroupLayouts append
    public PipelineBuilder WithSnippet(Snippet snippet)
    {
        if (snippet.VertexShader != null) _vertexShader = snippet.VertexShader;
        if (snippet.FragmentShader != null) _fragmentShader = snippet.FragmentShader;
        if (snippet.ShaderDefines != null)
        {
            _definesBuilder ??= ShaderDefines.NewBuilder();
            foreach (var kv in snippet.ShaderDefines.Values)
                _definesBuilder.Define(kv.Key, kv.Value);
            foreach (var flag in snippet.ShaderDefines.Flags)
                _definesBuilder.Define(flag);
        }
        if (snippet.BindGroupLayouts != null)
        {
            _bindGroupLayouts ??= new List<BindGroupLayout>();
            _bindGroupLayouts.AddRange(snippet.BindGroupLayouts);
        }
        if (snippet.OverrideDepthStencilState)
        {
            _depthStencilState = snippet.DepthStencilState;
            _depthStencilStateSet = true;
        }
        if (snippet.Cull != null) _cull = snippet.Cull;
        if (snippet.PolygonMode != null) _polygonMode = snippet.PolygonMode;
        if (snippet.PrimitiveTopology != null) _primitiveTopology = snippet.PrimitiveTopology;
        if (snippet.PushConstantSize != null) _pushConstantSize = snippet.PushConstantSize;
        for (int i = 0; i < snippet.ColorTargetStates.Count; i++)
        {
            var s = snippet.ColorTargetStates[i];
            if (_colorTargetStates[i] == null && s != null)
                _colorTargetStates[i] = s;
        }
        _activeColorTargetStateCount = Math.Max(_activeColorTargetStateCount, snippet.ColorTargetStates.Count);
        for (int i = 0; i < snippet.VertexFormatPerBuffer.Count; i++)
        {
            var vf = snippet.VertexFormatPerBuffer[i];
            if (vf != null)
                _vertexFormatPerBuffer[i] = vf;
        }
        return this;
    }

    public Snippet BuildSnippet()
    {
        //Only the declared slots are emitted; padding to the 16 slot ceiling would make every snippet report
        //16 color targets, and a pipeline built from it would no longer match a single attachment render pass
        var colorTargets = new ColorTargetState?[_activeColorTargetStateCount];
        for (int i = 0; i < _activeColorTargetStateCount; i++)
            colorTargets[i] = _colorTargetStates[i];
        var vertexFormats = new VertexFormat?[MaxVertexBindings];
        for (int i = 0; i < MaxVertexBindings; i++)
            vertexFormats[i] = _vertexFormatPerBuffer[i];
        return new Snippet(
            _vertexShader,
            _fragmentShader,
            _definesBuilder?.Build(),
            _bindGroupLayouts != null ? new ReadOnlyCollection<BindGroupLayout>(_bindGroupLayouts) : null,
            new ReadOnlyCollection<ColorTargetState?>(colorTargets),
            _depthStencilState,
            _polygonMode,
            _cull,
            new ReadOnlyCollection<VertexFormat?>(vertexFormats),
            _primitiveTopology,
            _pushConstantSize,
            _depthStencilStateSet);
    }

    public RenderPipeline Build()
    {
        if (_location == null) throw new InvalidOperationException("Missing location");
        if (_vertexShader == null) throw new InvalidOperationException("Missing vertex shader");
        if (_fragmentShader == null) throw new InvalidOperationException("Missing fragment shader");
        if (_primitiveTopology == null) throw new InvalidOperationException("Missing primitive topology");

        IReadOnlyList<ColorTargetState> colorTargets;
        if (_activeColorTargetStateCount == 0)
        {
            colorTargets = new[] { ColorTargetState.DEFAULT };
        }
        else
        {
            var arr = new ColorTargetState[_activeColorTargetStateCount];
            BlendFunction? lastBlend = null;
            for (int i = 0; i < _activeColorTargetStateCount; i++)
            {
                arr[i] = _colorTargetStates[i] ?? default;
                if (_colorTargetStates[i] is { } cs && cs.BlendFunction is { } blend)
                {
                    if (lastBlend is { } lb && !lb.Equals(blend))
                        throw new InvalidOperationException("Blend functions must currently be the same for all color targets");
                    lastBlend = blend;
                }
            }
            colorTargets = arr;
        }

        int attribCount = 0;
        foreach (var vf in _vertexFormatPerBuffer)
            if (vf != null) attribCount += vf.Elements.Count;
        if (attribCount > MaxVertexAttributes)
            throw new InvalidOperationException("Binding more than 16 vertex attributes is not supported");

        var vertexFormats = new VertexFormat?[MaxVertexBindings];
        for (int i = 0; i < MaxVertexBindings; i++)
            vertexFormats[i] = _vertexFormatPerBuffer[i];

        return new RenderPipeline(
            _location,
            _vertexShader,
            _fragmentShader,
            _definesBuilder?.Build() ?? ShaderDefines.NewBuilder().Build(),
            _bindGroupLayouts != null ? new ReadOnlyCollection<BindGroupLayout>(_bindGroupLayouts) : Array.Empty<BindGroupLayout>(),
            colorTargets,
            _depthStencilState,
            _polygonMode ?? PolygonMode.Fill,
            _cull ?? true,
            new ReadOnlyCollection<VertexFormat?>(vertexFormats),
            _primitiveTopology!.Value,
            _pushConstantSize ?? 0);
    }
}
