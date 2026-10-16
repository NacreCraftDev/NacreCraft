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
using NacreCraft.Client.Blaze3d;
namespace NacreCraft.Client.Blaze3d.Pipeline;

//UniformDescription uniform description, maps to vanilla UniformDescription record
//TEXEL_BUFFER must specify a GpuFormat; for other types GpuFormat is null
public readonly record struct UniformDescription(string Name, UniformType Type, GpuFormat? GpuFormat)
{
    public UniformDescription(string name, UniformType type) : this(name, type, null)
    {
        if (type == UniformType.TexelBuffer)
            throw new ArgumentException("Texel buffer needs a texture format");
    }

    public UniformDescription(string name, GpuFormat format) : this(name, UniformType.TexelBuffer, format) { }
}

//BindGroupLayout bind group layout, maps to vanilla BindGroupLayout
//Describes a set of sampler and uniform bindings for RenderPipeline to allocate a descriptor set layout at compile time
public sealed class BindGroupLayout
{
    public IReadOnlyList<string> Samplers { get; }
    public IReadOnlyList<UniformDescription> Uniforms { get; }

    private BindGroupLayout(IReadOnlyList<string> samplers, IReadOnlyList<UniformDescription> uniforms)
    {
        Samplers = samplers;
        Uniforms = uniforms;
    }

    public static Builder Create() => new();

    //FlattenSamplers sampler names across layouts in declaration order, maps to vanilla flattenSamplers
    public static List<string> FlattenSamplers(IReadOnlyList<BindGroupLayout> bindGroupLayouts)
    {
        var flattened = new List<string>();
        foreach (var layout in bindGroupLayouts)
            flattened.AddRange(layout.Samplers);
        return flattened;
    }

    //FlattenUniforms uniform descriptions across layouts in declaration order, maps to vanilla flattenUniforms
    public static List<UniformDescription> FlattenUniforms(IReadOnlyList<BindGroupLayout> bindGroupLayouts)
    {
        var flattened = new List<UniformDescription>();
        foreach (var layout in bindGroupLayouts)
            flattened.AddRange(layout.Uniforms);
        return flattened;
    }

    //EnsureCompatible rejects duplicate bind names across layouts, maps to vanilla ensureCompatible
    //A name may only be bound once, otherwise the named binding in RenderPass becomes ambiguous
    public static void EnsureCompatible(IReadOnlyList<BindGroupLayout> bindGroupLayouts)
    {
        var names = new HashSet<string>();
        for (var layoutIndex = 0; layoutIndex < bindGroupLayouts.Count; layoutIndex++)
        {
            foreach (var uniform in bindGroupLayouts[layoutIndex].Uniforms)
            {
                if (names.Add(uniform.Name)) continue;
                throw new ArgumentException($"Duplicate bind name '{uniform.Name}' in bind group layout {layoutIndex}");
            }
            foreach (var sampler in bindGroupLayouts[layoutIndex].Samplers)
            {
                if (names.Add(sampler)) continue;
                throw new ArgumentException($"Duplicate bind name '{sampler}' in bind group layout {layoutIndex}");
            }
        }
    }

    public sealed class Builder
    {
        private readonly List<string> _samplers = new();
        private readonly List<UniformDescription> _uniforms = new();

        public Builder AddSampler(string name)
        {
            _samplers.Add(name);
            return this;
        }

        public Builder AddUniform(string name, UniformType type)
        {
            _uniforms.Add(new UniformDescription(name, type));
            return this;
        }

        public Builder AddUniform(string name, UniformType type, GpuFormat format)
        {
            _uniforms.Add(new UniformDescription(name, type, format));
            return this;
        }

        public BindGroupLayout Build() => new(_samplers, _uniforms);
    }
}
