using System.Numerics;
using Silk.NET.Vulkan;
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

namespace NacreCraft.Client.Blaze3d.Platform;

//VulkanTriangleApp Vulkan triangle PoC main program
//Extends VulkanAppBase and only implements pipeline creation and command recording; the rest is handled by the base
public sealed unsafe class VulkanTriangleApp : VulkanAppBase
{
    private VulkanRenderPipeline _pipeline = null!;

    public VulkanTriangleApp() : base(800, 600) { }

    protected override string WindowTitle => "NacreCraft.Gpu.Vulkan Triangle PoC";

    //OnCreatePipelineResources creates the triangle pipeline using the built-in SpirvShaders shader
    protected override void OnCreatePipelineResources()
    {
        //The PoC uses the built-in SpirvShaders shader and leaves description null, using VulkanRenderPipeline defaults
        var description = new RenderPipelineDescription();
        _pipeline = new VulkanRenderPipeline(_vkDevice.Api, _vkDevice.Device, _swapchainImageFormat, _swapchainExtent, description);
    }

    //OnRecordCommandBuffer records one triangle into the scene texture
    protected override void OnRecordCommandBuffer(CommandEncoder encoder, GpuTextureView sceneView)
    {
        var descriptor = RenderPassDescriptor.Create(() => "triangle")
            .WithColorAttachment(sceneView, new Vector4(0.1f, 0.1f, 0.1f, 1f))
            .WithRenderArea(new NacreCraft.Client.Blaze3d.Systems.RenderPass.RenderArea(0, 0, (int)_swapchainExtent.Width, (int)_swapchainExtent.Height));
        var pass = (VulkanRenderPass)encoder.Backend.CreateRenderPass(descriptor);
        pass.SetCompiledPipeline(_pipeline);
        pass.Draw(3, 1, 0, 0);
        encoder.SubmitRenderPass();
    }

    //OnCleanupPipelineResources destroys pipeline resources
    protected override void OnCleanupPipelineResources()
    {
        _pipeline.Dispose();
    }
}
