using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;

using CompiledRenderPipeline = NacreCraft.Client.Blaze3d.Pipeline.CompiledRenderPipeline;
using RenderPipelineDescription = NacreCraft.Client.Blaze3d.Pipeline.RenderPipelineDescription;
using RenderPipeline = NacreCraft.Client.Blaze3d.Pipeline.RenderPipeline;
using PipelineCache = NacreCraft.Client.Blaze3d.Pipeline.PipelineCache;
using BindGroupLayout = NacreCraft.Client.Blaze3d.Pipeline.BindGroupLayout;
using ColorTargetState = NacreCraft.Client.Blaze3d.Pipeline.ColorTargetState;
using DepthStencilState = NacreCraft.Client.Blaze3d.Pipeline.DepthStencilState;
using NacreCraft.Client.Blaze3d.Pipeline;
using CompareOp = Silk.NET.Vulkan.CompareOp;
using BlendFactor = Silk.NET.Vulkan.BlendFactor;
using BlendOp = Silk.NET.Vulkan.BlendOp;
using PolygonMode = Silk.NET.Vulkan.PolygonMode;
using PrimitiveTopology = Silk.NET.Vulkan.PrimitiveTopology;
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
namespace NacreCraft.Client.Blaze3d.Vulkan;

//VulkanShader Vulkan backend SPIR-V shader module
//Wraps VkShaderModule created by VulkanDevice.CreateShader
public sealed unsafe class VulkanShader : GpuShader
{
    private readonly Vk _vk;
    private readonly Device _device;
    private ShaderModule _module;
    private bool _disposed;

    public ShaderModule Handle => _module;

    internal VulkanShader(Vk vk, Device device, GpuShaderStage stage, byte[] spirvCode, string entryPoint)
        : base(stage, spirvCode, entryPoint)
    {
        _vk = vk;
        _device = device;
        var createInfo = new ShaderModuleCreateInfo
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)spirvCode.Length
        };
        fixed (byte* codePtr = spirvCode)
        {
            createInfo.PCode = (uint*)codePtr;
            if (_vk.CreateShaderModule(_device, &createInfo, null, out _module) != Result.Success)
                throw new InvalidOperationException("ShaderModule creation failed");
        }
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _vk.DestroyShaderModule(_device, _module, null);
        _disposed = true;
    }
}
