using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
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
using NacreCraft.Client.Blaze3d;

namespace NacreCraft.Client.Blaze3d.Pipeline;

//CompiledRenderPipeline compiled render pipeline, maps to the vanilla CompiledRenderPipeline interface
//Produced by IGpuDevice.PrecompilePipeline compiling a declarative RenderPipeline
//VulkanRenderPipeline is its Vulkan backend implementation holding a VkPipeline handle
public abstract class CompiledRenderPipeline : IDisposable
{
    public RenderPipelineDescription Description { get; }

    protected CompiledRenderPipeline(RenderPipelineDescription description)
    {
        Description = description;
    }

    public virtual void Dispose() { }
}

//GpuVertexAttribute vertex attribute description
//The format is a GpuFormat, the same description VertexFormatElement carries
public sealed class GpuVertexAttribute
{
    public int Location { get; set; }
    public GpuFormat Format { get; set; }
    public int Offset { get; set; }
}

//GpuVertexBinding vertex binding description
public sealed class GpuVertexBinding
{
    public int Binding { get; set; }
    public int Stride { get; set; }
    public List<GpuVertexAttribute> Attributes { get; set; } = new();
}

//GpuPrimitiveTopology primitive topology
public enum GpuPrimitiveTopology
{
    TriangleList,
    TriangleStrip,
    LineList,
    PointList
}

//RenderPipelineDescription render pipeline description
//Subclasses create the underlying pipeline from this description
public sealed class RenderPipelineDescription
{
    //VertexShaderSource vertex shader SPIR-V bytecode
    //null means use the PoC's built-in triangle shader
    public byte[]? VertexShaderSpirv { get; set; }
    //FragmentShaderSource fragment shader SPIR-V bytecode
    public byte[]? FragmentShaderSpirv { get; set; }
    //VertexBindings vertex layout bindings; an empty list means no vertex input and uses gl_VertexIndex
    public List<GpuVertexBinding> VertexBindings { get; set; } = new();
    //DescriptorLayouts descriptor set layout list for uniform buffer/sampler binding
    //An empty list means the pipeline needs no external resource bindings
    public List<GpuDescriptorLayout> DescriptorLayouts { get; set; } = new();
    //DescriptorLayoutDescriptions descriptor set layout description list, used after converting a declarative RenderPipeline to a description for the device to compile
    //When non-empty, the PrecompilePipeline implementation compiles them into GpuDescriptorLayouts and fills in DescriptorLayouts
    public List<GpuDescriptorLayoutDescription> DescriptorLayoutDescriptions { get; set; } = new();
    //DescriptorBindingNames binding names per descriptor set, parallel to DescriptorLayouts; RenderPass binds resources by these names
    public List<List<string>> DescriptorBindingNames { get; set; } = new();
    //Topology primitive topology
    public GpuPrimitiveTopology Topology { get; set; } = GpuPrimitiveTopology.TriangleList;
    //DepthTestEnabled depth test
    public bool DepthTestEnabled { get; set; }
    //DepthCompareOp depth compare function, default Less; VulkanRenderPipeline reads this field instead of hardcoding
    public CompareOp DepthCompareOp { get; set; } = CompareOp.Less;
    //BlendEnabled alpha blending
    public bool BlendEnabled { get; set; }
    //DynamicScissorEnabled enables VK_DYNAMIC_STATE_SCISSOR, setting the scissor at runtime with vkCmdSetScissor
    //GUI pipelines set true; triangle/cube pipelines keep the false default
    public bool DynamicScissorEnabled { get; set; }
    //PushConstantSize size in bytes of the push constant block; 0 means the pipeline has none
    //Terrain pipelines use it to carry the section origin without touching a uniform buffer
    public int PushConstantSize { get; set; }
    //TargetFormat target color attachment format; null means use the swapchain format
    public GpuFormat? TargetFormat { get; set; }
    //TargetExtent target extent; 0 means use the swapchain extent
    public int TargetWidth { get; set; }
    public int TargetHeight { get; set; }
    //ClearColor clear color RGBA
    public (float R, float G, float B, float A) ClearColor { get; set; } = (0f, 0f, 0f, 1f);

    //FromDeclaration converts a declarative RenderPipeline into a RenderPipelineDescription
    //Stage 8 loads embedded GLSL via ShaderManager, compiles to SPIR-V and injects ShaderDefines
    public static RenderPipelineDescription FromDeclaration(RenderPipeline declaration, ShaderManager shaderManager)
    {
        var desc = new RenderPipelineDescription
        {
            VertexShaderSpirv = shaderManager.LoadVertexShader(declaration.VertexShader, declaration.ShaderDefines),
            FragmentShaderSpirv = shaderManager.LoadFragmentShader(declaration.FragmentShader, declaration.ShaderDefines),
            Topology = ToLegacyTopology(declaration.PrimitiveTopology),
            DepthTestEnabled = declaration.DepthStencilState != null,
            DepthCompareOp = declaration.DepthStencilState?.DepthTest ?? CompareOp.Less,
            BlendEnabled = declaration.ColorTargetStates.Count > 0 && declaration.ColorTargetStates[0].BlendFunction != null,
            DynamicScissorEnabled = true,
            PushConstantSize = declaration.PushConstantSize,
            TargetFormat = ToLegacyFormat(declaration.ColorTargetStates[0].Format)
        };

        foreach (var b in declaration.BindGroupLayouts)
        {
            var layoutDesc = new GpuDescriptorLayoutDescription();
            var names = new List<string>();
            foreach (var s in b.Samplers)
            {
                layoutDesc.Bindings.Add(new GpuDescriptorBinding
                {
                    Binding = layoutDesc.Bindings.Count,
                    DescriptorType = GpuDescriptorType.CombinedImageSampler,
                    StageFlags = GpuShaderStageFlags.AllGraphics
                });
                names.Add(s);
            }
            foreach (var u in b.Uniforms)
            {
                layoutDesc.Bindings.Add(new GpuDescriptorBinding
                {
                    Binding = layoutDesc.Bindings.Count,
                    DescriptorType = GpuDescriptorType.UniformBuffer,
                    StageFlags = GpuShaderStageFlags.AllGraphics
                });
                names.Add(u.Name);
            }
            desc.DescriptorLayoutDescriptions.Add(layoutDesc);
            desc.DescriptorBindingNames.Add(names);
        }

        for (int i = 0; i < declaration.VertexFormatPerBuffer.Count; i++)
        {
            var vf = declaration.VertexFormatPerBuffer[i];
            if (vf == null) continue;
            var binding = new GpuVertexBinding { Binding = i, Stride = vf.VertexSize };
            int location = 0;
            foreach (var e in vf.Elements)
            {
                binding.Attributes.Add(new GpuVertexAttribute
                {
                    Location = location++,
                    Format = e.Format,
                    Offset = e.Offset
                });
            }
            desc.VertexBindings.Add(binding);
        }

        return desc;
    }

    private static GpuPrimitiveTopology ToLegacyTopology(PrimitiveTopology t) => t switch
    {
        PrimitiveTopology.TriangleList => GpuPrimitiveTopology.TriangleList,
        PrimitiveTopology.TriangleStrip => GpuPrimitiveTopology.TriangleStrip,
        PrimitiveTopology.Lines => GpuPrimitiveTopology.LineList,
        PrimitiveTopology.Points => GpuPrimitiveTopology.PointList,
        _ => GpuPrimitiveTopology.TriangleList
    };

    private static GpuFormat ToLegacyFormat(GpuFormat f) => f switch
    {
        GpuFormat.Rgba8Unorm => GpuFormat.Rgba8Unorm,
        GpuFormat.Bgra8Unorm => GpuFormat.Bgra8Unorm,
        GpuFormat.R8Unorm => GpuFormat.R8Unorm,
        GpuFormat.D32Float => GpuFormat.D32Float,
        _ => GpuFormat.Rgba8Unorm
    };
}
