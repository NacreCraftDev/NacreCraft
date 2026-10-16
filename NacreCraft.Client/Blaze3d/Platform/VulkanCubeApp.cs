using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Veldrid.SPIRV;
using ShaderStages = Veldrid.ShaderStages;
using VkFormat = Silk.NET.Vulkan.Format;
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

namespace NacreCraft.Client.Blaze3d.Platform;

//VulkanCubeApp 3D cube rendering example
//Validates the full chain of depth test+MVP uniform buffer+indexed drawing+descriptor set binding
//Updates the model rotation matrix every frame to demonstrate dynamic uniform buffer updates
public sealed unsafe class VulkanCubeApp : VulkanAppBase
{
    //CubeVertex cube vertex vec3 position + vec3 color, 24 bytes total
    [StructLayout(LayoutKind.Sequential)]
    private struct CubeVertex
    {
        public Vector3 Position;
        public Vector3 Color;
        public CubeVertex(float x, float y, float z, float r, float g, float b)
        {
            Position = new Vector3(x, y, z);
            Color = new Vector3(r, g, b);
        }
    }

    //MvpUniform model-view-projection matrix, uploaded column-major to the shader
    //System.Numerics.Matrix4x4 is row-major and needs a transpose before upload
    [StructLayout(LayoutKind.Sequential)]
    private struct MvpUniform
    {
        public Matrix4x4 Model;
        public Matrix4x4 View;
        public Matrix4x4 Proj;
    }

    private VulkanRenderPipeline _pipeline = null!;
    private VulkanBuffer _vertexBuffer = null!;
    private VulkanBuffer _indexBuffer = null!;
    private VulkanBuffer _uniformBuffer = null!;
    private VulkanDescriptorLayout _descriptorLayout = null!;
    private VulkanImage _depthImage = null!;
    private GpuTextureView _depthView = null!;
    private float _rotation;

    public VulkanCubeApp() : base(800, 600) { }

    protected override string WindowTitle => "NacreCraft.Gpu Vulkan Cube PoC";

    //4.3 rework to dynamic rendering no longer needs GetFramebufferRenderPass and CreateFramebuffers
    //The depth attachment is passed by OnRecordCommandBuffer as _depthImage to BeginRenderPass

    protected override void OnCreatePipelineResources()
    {
        CreateDepthImage();
        CreateVertexBuffer();
        CreateUniformBuffer();
        CreateDescriptorSet();
        CreatePipeline();
    }

    //OnSwapchainRecreated rebuilds the depth image and pipeline after swapchain recreation since they depend on the extent
    //The descriptor set is already allocated and only needs the uniform buffer rebound
    protected override void OnSwapchainRecreated()
    {
        _depthImage.Dispose();
        _depthView.Dispose();
        _pipeline.Dispose();
        CreateDepthImage();
        CreatePipeline();
    }

    private void CreateDepthImage()
    {
        _depthImage = (VulkanImage)_device.CreateTexture(null, GpuTexture.UsageRenderAttachment, GpuFormat.D32Float, (int)_swapchainExtent.Width, (int)_swapchainExtent.Height, 1, 1);
        //Uploading empty pixels triggers the Undefined->DepthStencilAttachmentOptimal layout transition
        _depthImage.Upload(ReadOnlySpan<byte>.Empty);
        _depthView = _device.CreateTextureView(_depthImage);
    }

    private void CreateVertexBuffer()
    {
        CubeVertex[] vertices =
        {
            new(-0.5f, -0.5f, -0.5f, 1f, 0f, 0f),
            new( 0.5f, -0.5f, -0.5f, 0f, 1f, 0f),
            new( 0.5f,  0.5f, -0.5f, 0f, 0f, 1f),
            new(-0.5f,  0.5f, -0.5f, 1f, 1f, 0f),
            new(-0.5f, -0.5f,  0.5f, 1f, 0f, 1f),
            new( 0.5f, -0.5f,  0.5f, 0f, 1f, 1f),
            new( 0.5f,  0.5f,  0.5f, 1f, 1f, 1f),
            new(-0.5f,  0.5f,  0.5f, 0f, 0f, 0f)
        };
        int size = vertices.Length * sizeof(CubeVertex);
        _vertexBuffer = (VulkanBuffer)_device.CreateBuffer(null, GpuBuffer.UsageVertex | GpuBuffer.UsageCopyDst, size);
        _vertexBuffer.Upload<CubeVertex>(vertices);
        ushort[] indices =
        {
            0, 1, 2,  0, 2, 3,
            4, 5, 6,  4, 6, 7,
            3, 2, 6,  3, 6, 7,
            0, 5, 1,  0, 4, 5,
            0, 3, 7,  0, 7, 4,
            1, 5, 6,  1, 6, 2
        };
        int indexSize = indices.Length * sizeof(ushort);
        _indexBuffer = (VulkanBuffer)_device.CreateBuffer(null, GpuBuffer.UsageIndex | GpuBuffer.UsageCopyDst, indexSize);
        _indexBuffer.Upload<ushort>(indices);
    }

    private void CreateUniformBuffer()
    {
        _uniformBuffer = (VulkanBuffer)_device.CreateBuffer(null, GpuBuffer.UsageUniform | GpuBuffer.UsageMapWrite, sizeof(MvpUniform));
    }

    private void CreateDescriptorSet()
    {
        var layoutDesc = new GpuDescriptorLayoutDescription();
        layoutDesc.Bindings.Add(new GpuDescriptorBinding
        {
            Binding = 0,
            DescriptorType = GpuDescriptorType.UniformBuffer,
            StageFlags = GpuShaderStageFlags.Vertex
        });
        _descriptorLayout = (VulkanDescriptorLayout)_vkDevice.CreateDescriptorLayout(layoutDesc);
    }

    private void CreatePipeline()
    {
        var vertSpv = CompileGlsl(VertexShaderSource, ShaderStages.Vertex);
        var fragSpv = CompileGlsl(FragmentShaderSource, ShaderStages.Fragment);
        var desc = new RenderPipelineDescription
        {
            VertexShaderSpirv = vertSpv,
            FragmentShaderSpirv = fragSpv,
            Topology = GpuPrimitiveTopology.TriangleList,
            BlendEnabled = false,
            DepthTestEnabled = true,
            TargetFormat = GpuFormat.Bgra8Unorm,
            TargetWidth = (int)_swapchainExtent.Width,
            TargetHeight = (int)_swapchainExtent.Height
        };
        desc.VertexBindings.Add(new GpuVertexBinding
        {
            Binding = 0,
            Stride = sizeof(CubeVertex),
            Attributes =
            {
                new GpuVertexAttribute { Location = 0, Format = GpuFormat.Rgb32Float, Offset = 0 },
                new GpuVertexAttribute { Location = 1, Format = GpuFormat.Rgb32Float, Offset = 12 }
            }
        });
        desc.DescriptorLayouts.Add(_descriptorLayout);
        //The uniform block is declared as "ubo" in the shader; render passes bind it by that name
        desc.DescriptorBindingNames.Add(new List<string> { "ubo" });
        var fmt = VulkanRenderPipeline.ToVkFormat(desc.TargetFormat ?? GpuFormat.Bgra8Unorm);
        _pipeline = new VulkanRenderPipeline(_vkDevice.Api, _vkDevice.Device, fmt, _swapchainExtent, desc);
    }

    //UpdateUniformBuffer updates the model rotation matrix every frame with view/proj fixed
    //Matrix4x4 is row-major; transpose to column-major before upload to match GLSL mat4
    private void UpdateUniformBuffer()
    {
        _rotation += 0.01f;
        var model = Matrix4x4.CreateRotationY(_rotation) * Matrix4x4.CreateRotationX(_rotation * 0.5f);
        var view = Matrix4x4.CreateLookAt(new Vector3(2, 2, 2), Vector3.Zero, Vector3.UnitY);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(60f * MathF.PI / 180f, (float)_swapchainExtent.Width / _swapchainExtent.Height, 0.1f, 10f);
        //Vulkan clip space Z range [0,1] differs from OpenGL [-1,1], so M33/M43 need correction
        proj.M33 = 0.1f / (0.1f - 10f);
        proj.M43 = (0.1f * 10f) / (0.1f - 10f);
        //The Vulkan framebuffer has Y down; flipping Y shows the cube upright
        proj.M22 *= -1;
        var mvp = new MvpUniform
        {
            Model = Matrix4x4.Transpose(model),
            View = Matrix4x4.Transpose(view),
            Proj = Matrix4x4.Transpose(proj)
        };
        _uniformBuffer.Upload<MvpUniform>(new[] { mvp });
    }

    //OnRecordCommandBuffer records the cube into the scene texture with a depth attachment
    protected override void OnRecordCommandBuffer(CommandEncoder encoder, GpuTextureView sceneView)
    {
        UpdateUniformBuffer();
        var descriptor = RenderPassDescriptor.Create(() => "cube")
            .WithColorAttachment(sceneView, new Vector4(0.05f, 0.05f, 0.08f, 1f))
            .WithDepthAttachment(_depthView, 1.0)
            .WithRenderArea(new NacreCraft.Client.Blaze3d.Systems.RenderPass.RenderArea(0, 0, (int)_swapchainExtent.Width, (int)_swapchainExtent.Height));
        var pass = (VulkanRenderPass)encoder.Backend.CreateRenderPass(descriptor);
        pass.SetCompiledPipeline(_pipeline);
        pass.SetVertexBuffer(0, _vertexBuffer.Slice());
        pass.SetIndexBuffer(_indexBuffer, NacreCraft.Client.Blaze3d.IndexType.Short);
        pass.SetUniform("ubo", _uniformBuffer);
        pass.DrawIndexed(36, 1, 0, 0, 0);
        encoder.SubmitRenderPass();
    }

    protected override void OnCleanupPipelineResources()
    {
        _descriptorLayout.Dispose();
        _uniformBuffer.Dispose();
        _indexBuffer.Dispose();
        _vertexBuffer.Dispose();
        _depthView.Dispose();
        _depthImage.Dispose();
        _pipeline.Dispose();
    }

    private static byte[] CompileGlsl(string source, ShaderStages stage)
    {
        var fileName = stage == ShaderStages.Vertex ? "vert.glsl" : "frag.glsl";
        var result = SpirvCompilation.CompileGlslToSpirv(source, fileName, stage, new GlslCompileOptions());
        return result.SpirvBytes;
    }

    private const string VertexShaderSource = @"
#version 450
layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(binding = 0) uniform UniformBufferObject {
    mat4 model;
    mat4 view;
    mat4 proj;
} ubo;
layout(location = 0) out vec3 fragColor;
void main() {
    gl_Position = ubo.proj * ubo.view * ubo.model * vec4(inPosition, 1.0);
    fragColor = inColor;
}
";

    private const string FragmentShaderSource = @"
#version 450
layout(location = 0) in vec3 fragColor;
layout(location = 0) out vec4 outColor;
void main() {
    outColor = vec4(fragColor, 1.0);
}
";
}
