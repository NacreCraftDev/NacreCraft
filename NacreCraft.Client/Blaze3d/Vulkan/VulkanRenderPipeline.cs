using Silk.NET.Core.Native;
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
using NacreCraft.Client.Blaze3d;
namespace NacreCraft.Client.Blaze3d.Vulkan;

//ClearColorValue float RGBA clear color
public readonly record struct ClearColorValueRGBA(float R, float G, float B, float A);

//VulkanRenderPipeline Vulkan backend render pipeline
//4.3 rework removes the traditional RenderPass and uses dynamic rendering; pipeline creation uses the PipelineRenderingCreateInfoKHR PNext chain
//The attachment ImageView is passed by VulkanRenderPass (the Vulkan IRenderPass implementation) at CmdBeginRenderingKHR
public sealed unsafe class VulkanRenderPipeline : CompiledRenderPipeline
{
    private readonly Vk _vk;
    private readonly Device _device;
    private readonly Format _colorFormat;
    private readonly Silk.NET.Vulkan.Pipeline _pipeline;
    private readonly PipelineLayout _pipelineLayout;
    private readonly Extent2D _extent;
    private readonly ShaderModule _vertModule;
    private readonly ShaderModule _fragModule;
    //BindingMap resolves a declared binding name to its descriptor set and binding index, used by RenderPass name-based binding
    private readonly Dictionary<string, (uint Set, uint Binding)> _bindingMap = new();
    private bool _disposed;

    public Silk.NET.Vulkan.Pipeline Pipeline => _pipeline;
    public PipelineLayout PipelineLayout => _pipelineLayout;
    public Extent2D Extent => _extent;
    //ColorFormat color attachment format for VulkanRenderPass to build RenderingAttachmentInfoKHR
    public Format ColorFormat => _colorFormat;
    public ClearColorValueRGBA ClearColor { get; set; }

    //TryGetBinding resolves a declared binding name to its descriptor set and binding index
    public bool TryGetBinding(string name, out uint set, out uint binding)
    {
        if (_bindingMap.TryGetValue(name, out var location))
        {
            set = location.Set;
            binding = location.Binding;
            return true;
        }
        set = 0;
        binding = 0;
        return false;
    }

    internal VulkanRenderPipeline(
        Vk vk,
        Device device,
        Format swapchainFormat,
        Extent2D swapchainExtent,
        RenderPipelineDescription description) : base(description)
    {
        _vk = vk;
        _device = device;
        _extent = swapchainExtent;
        _colorFormat = swapchainFormat;
        ClearColor = new(description.ClearColor.R, description.ClearColor.G, description.ClearColor.B, description.ClearColor.A);
        var vertCode = description.VertexShaderSpirv ?? SpirvShaders.VertexShader;
        var fragCode = description.FragmentShaderSpirv ?? SpirvShaders.FragmentShader;
        _vertModule = CreateShaderModule(vertCode);
        _fragModule = CreateShaderModule(fragCode);
        _pipelineLayout = CreatePipelineLayout(description);
        _pipeline = CreateGraphicsPipeline(description);
        for (int set = 0; set < description.DescriptorBindingNames.Count; set++)
        {
            var names = description.DescriptorBindingNames[set];
            for (int binding = 0; binding < names.Count; binding++)
                _bindingMap[names[binding]] = ((uint)set, (uint)binding);
        }
    }

    //FromDescription creates the pipeline from the description; the target format and extent default to B8G8R8A8Unorm/800x600
    public static VulkanRenderPipeline FromDescription(VulkanDevice device, RenderPipelineDescription description)
    {
        var fmt = description.TargetFormat ?? GpuFormat.Bgra8Unorm;
        var extent = new Extent2D
        {
            Width = (uint)(description.TargetWidth > 0 ? description.TargetWidth : 800),
            Height = (uint)(description.TargetHeight > 0 ? description.TargetHeight : 600)
        };
        return new VulkanRenderPipeline(device.Api, device.Device, ToVkFormat(fmt), extent, description);
    }

    private ShaderModule CreateShaderModule(byte[] code)
    {
        var createInfo = new ShaderModuleCreateInfo
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)code.Length
        };
        fixed (byte* codePtr = code)
        {
            createInfo.PCode = (uint*)codePtr;
            ShaderModule module;
            if (_vk.CreateShaderModule(_device, &createInfo, null, &module) != Result.Success)
            {
                throw new InvalidOperationException("ShaderModule creation failed");
            }
            return module;
        }
    }

    //CreatePipelineLayout takes the VkDescriptorSetLayout array from description.DescriptorLayouts to create a PipelineLayout
    //With an empty list SetLayoutCount=0 and an empty layout is still created for pure gl_VertexIndex shaders
    private PipelineLayout CreatePipelineLayout(RenderPipelineDescription description)
    {
        var layouts = description.DescriptorLayouts;
        var layoutHandles = new DescriptorSetLayout[layouts.Count];
        for (int i = 0; i < layouts.Count; i++)
        {
            if (layouts[i] is not VulkanDescriptorLayout vkLayout)
                throw new ArgumentException("DescriptorLayout must be a VulkanDescriptorLayout");
            layoutHandles[i] = vkLayout.Handle;
        }
        fixed (DescriptorSetLayout* p = layoutHandles)
        {
            //Terrain pipelines declare a push constant block for the section origin; the range covers both stages
            var pushRange = new PushConstantRange
            {
                StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                Offset = 0,
                Size = (uint)description.PushConstantSize
            };
            var hasPushConstant = description.PushConstantSize > 0;
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)layouts.Count,
                PSetLayouts = p,
                PushConstantRangeCount = hasPushConstant ? 1u : 0u,
                PPushConstantRanges = hasPushConstant ? &pushRange : null
            };
            PipelineLayout layout;
            if (_vk.CreatePipelineLayout(_device, &layoutInfo, null, &layout) != Result.Success)
            {
                throw new InvalidOperationException("PipelineLayout creation failed");
            }
            return layout;
        }
    }

    private Silk.NET.Vulkan.Pipeline CreateGraphicsPipeline(RenderPipelineDescription description)
    {
        var vertStage = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = _vertModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };
        var fragStage = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = _fragModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };
        var shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
        shaderStages[0] = vertStage;
        shaderStages[1] = fragStage;

        //The vertex input layout is built from description.VertexBindings
        //Silk.NET's VertexInputBindingDescription has managed fields so stackalloc T* is impossible; array + fixed is used
        var bindings = description.VertexBindings;
        var bindingDescsArray = new VertexInputBindingDescription[bindings.Count];
        var attrDescsList = new List<VertexInputAttributeDescription>();
        for (int i = 0; i < bindings.Count; i++)
        {
            bindingDescsArray[i] = new VertexInputBindingDescription
            {
                Binding = (uint)bindings[i].Binding,
                Stride = (uint)bindings[i].Stride,
                InputRate = VertexInputRate.Vertex
            };
            foreach (var attr in bindings[i].Attributes)
            {
                attrDescsList.Add(new VertexInputAttributeDescription
                {
                    Location = (uint)attr.Location,
                    Binding = (uint)bindings[i].Binding,
                    Format = ToVkFormat(attr.Format),
                    Offset = (uint)attr.Offset
                });
            }
        }
        var attrDescsArray = attrDescsList.ToArray();

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = ToVkTopology(description.Topology),
            PrimitiveRestartEnable = Vk.False
        };
        var viewport = new Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = _extent.Width,
            Height = _extent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        var scissor = new Rect2D { Offset = default, Extent = _extent };
        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            //With dynamic state the viewport/scissor come from vkCmdSetViewport/vkCmdSetScissor at render pass start
            PViewports = description.DynamicScissorEnabled ? null : &viewport,
            ScissorCount = 1,
            PScissors = description.DynamicScissorEnabled ? null : &scissor
        };
        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = Vk.False,
            RasterizerDiscardEnable = Vk.False,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1.0f,
            CullMode = CullModeFlags.None,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = Vk.False
        };
        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = Vk.False,
            RasterizationSamples = SampleCountFlags.Count1Bit
        };
        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = description.BlendEnabled ? Vk.True : Vk.False
        };
        if (description.BlendEnabled)
        {
            colorBlendAttachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
            colorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
            colorBlendAttachment.ColorBlendOp = BlendOp.Add;
            colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
            colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.Zero;
            colorBlendAttachment.AlphaBlendOp = BlendOp.Add;
        }
        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = Vk.False,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };
        colorBlending.BlendConstants[0] = 0.0f;
        colorBlending.BlendConstants[1] = 0.0f;
        colorBlending.BlendConstants[2] = 0.0f;
        colorBlending.BlendConstants[3] = 0.0f;

        //With depthTest=true depth test and write are enabled; the compare function is read from the description instead of hardcoded
        //DepthStencilState.DEFAULT uses GreaterOrEqual with reversed-Z clearDepth=0, but standard Vulkan [0,1] uses Less+clearDepth=1
        //The current Camera projection uses standard Vulkan depth Less, matching terrain pipelines; entity pipelines match with GreaterOrEqual
        var depthStencil = new PipelineDepthStencilStateCreateInfo
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = description.DepthTestEnabled ? Vk.True : Vk.False,
            DepthWriteEnable = description.DepthTestEnabled ? Vk.True : Vk.False,
            DepthCompareOp = ToVkCompareOp(description.DepthCompareOp),
            DepthBoundsTestEnable = Vk.False,
            MinDepthBounds = 0f,
            MaxDepthBounds = 1f,
            StencilTestEnable = Vk.False
        };

        Silk.NET.Vulkan.Pipeline pipeline;
        fixed (VertexInputBindingDescription* bindingDescs = bindingDescsArray)
        fixed (VertexInputAttributeDescription* attrDescs = attrDescsArray)
        {
            var vertexInputInfo = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = (uint)bindings.Count,
                PVertexBindingDescriptions = bindingDescs,
                VertexAttributeDescriptionCount = (uint)attrDescsList.Count,
                PVertexAttributeDescriptions = attrDescs
            };
            //PipelineRenderingCreateInfoKHR 4.3 rework for dynamic rendering pipeline creation
            //With RenderPass=null the driver creates the pipeline from this struct's attachment format, compatible with CmdBeginRenderingKHR
            var colorFormat = _colorFormat;
            var renderingInfo = new PipelineRenderingCreateInfoKHR
            {
                SType = StructureType.PipelineRenderingCreateInfo,
                ColorAttachmentCount = 1,
                PColorAttachmentFormats = &colorFormat,
                DepthAttachmentFormat = description.DepthTestEnabled ? Format.D32Sfloat : Format.Undefined,
                StencilAttachmentFormat = Format.Undefined
            };
            var pipelineInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &renderingInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                Layout = _pipelineLayout,
                //RenderPass=null takes the dynamic rendering path with the PNext chain providing PipelineRenderingCreateInfoKHR
                RenderPass = default,
                Subpass = 0,
                BasePipelineHandle = default
            };
            //Viewport and scissor are both dynamic so one pipeline serves any render extent
            //The count must cover every state in the array: with pScissors null the pipeline may only be created
            //when VK_DYNAMIC_STATE_SCISSOR is declared dynamic, otherwise the driver reads a null scissor array
            var dynamicStates = stackalloc DynamicState[2];
            dynamicStates[0] = DynamicState.Viewport;
            dynamicStates[1] = DynamicState.Scissor;
            var dynamicStateInfo = new PipelineDynamicStateCreateInfo
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = dynamicStates
            };
            if (description.DynamicScissorEnabled)
            {
                pipelineInfo.PDynamicState = &dynamicStateInfo;
            }
            if (_vk.CreateGraphicsPipelines(_device, default, 1, &pipelineInfo, null, &pipeline) != Result.Success)
            {
                throw new InvalidOperationException("GraphicsPipeline creation failed");
            }
        }
        SilkMarshal.Free((nint)vertStage.PName);
        SilkMarshal.Free((nint)fragStage.PName);
        return pipeline;
    }

    //ToVkFormat maps a GpuFormat to its Vulkan counterpart, covering color, depth and vertex attribute formats
    public static Format ToVkFormat(GpuFormat fmt) => fmt switch
    {
        GpuFormat.R8Unorm => Format.R8Unorm,
        GpuFormat.Rg8Unorm => Format.R8G8Unorm,
        GpuFormat.Rgb8Unorm => Format.R8G8B8Unorm,
        GpuFormat.Rgba8Unorm => Format.R8G8B8A8Unorm,
        GpuFormat.Bgra8Unorm => Format.B8G8R8A8Unorm,
        GpuFormat.Rgba8UnormSrgb => Format.R8G8B8A8Srgb,
        GpuFormat.Bgra8UnormSrgb => Format.B8G8R8A8Srgb,
        GpuFormat.R8Uint => Format.R8Uint,
        GpuFormat.R8Sint => Format.R8Sint,
        GpuFormat.Rg8Uint => Format.R8G8Uint,
        GpuFormat.Rg8Sint => Format.R8G8Sint,
        GpuFormat.Rgb8Uint => Format.R8G8B8Uint,
        GpuFormat.Rgb8Sint => Format.R8G8B8Sint,
        GpuFormat.Rgba8Uint => Format.R8G8B8A8Uint,
        GpuFormat.Rgba8Sint => Format.R8G8B8A8Sint,
        GpuFormat.R16Unorm => Format.R16Unorm,
        GpuFormat.Rg16Unorm => Format.R16G16Unorm,
        GpuFormat.Rgba16Unorm => Format.R16G16B16A16Unorm,
        GpuFormat.R16Uint => Format.R16Uint,
        GpuFormat.R16Sint => Format.R16Sint,
        GpuFormat.Rg16Uint => Format.R16G16Uint,
        GpuFormat.Rg16Sint => Format.R16G16Sint,
        GpuFormat.Rgba16Uint => Format.R16G16B16A16Uint,
        GpuFormat.Rgba16Sint => Format.R16G16B16A16Sint,
        GpuFormat.R32Uint => Format.R32Uint,
        GpuFormat.R32Sint => Format.R32Sint,
        GpuFormat.Rg32Uint => Format.R32G32Uint,
        GpuFormat.Rg32Sint => Format.R32G32Sint,
        GpuFormat.Rgba32Uint => Format.R32G32B32A32Uint,
        GpuFormat.Rgba32Sint => Format.R32G32B32A32Sint,
        GpuFormat.R32Float => Format.R32Sfloat,
        GpuFormat.Rg32Float => Format.R32G32Sfloat,
        GpuFormat.Rgb32Float => Format.R32G32B32Sfloat,
        GpuFormat.Rgba32Float => Format.R32G32B32A32Sfloat,
        GpuFormat.D32Float => Format.D32Sfloat,
        GpuFormat.D16Unorm => Format.D16Unorm,
        _ => throw new ArgumentOutOfRangeException(nameof(fmt), $"{fmt} has no Vulkan mapping yet")
    };

    private static Silk.NET.Vulkan.PrimitiveTopology ToVkTopology(GpuPrimitiveTopology topo) => topo switch
    {
        GpuPrimitiveTopology.TriangleList => PrimitiveTopology.TriangleList,
        GpuPrimitiveTopology.TriangleStrip => PrimitiveTopology.TriangleStrip,
        GpuPrimitiveTopology.LineList => PrimitiveTopology.LineList,
        GpuPrimitiveTopology.PointList => PrimitiveTopology.PointList,
        _ => throw new ArgumentOutOfRangeException(nameof(topo))
    };

    //ToVkCompareOp maps NacreCraft.Client.Blaze3d.Pipeline.CompareOp to Silk.NET.Vulkan.CompareOp
    private static Silk.NET.Vulkan.CompareOp ToVkCompareOp(NacreCraft.Client.Blaze3d.Pipeline.CompareOp op) => op switch
    {
        NacreCraft.Client.Blaze3d.Pipeline.CompareOp.Never => Silk.NET.Vulkan.CompareOp.Never,
        NacreCraft.Client.Blaze3d.Pipeline.CompareOp.Less => Silk.NET.Vulkan.CompareOp.Less,
        NacreCraft.Client.Blaze3d.Pipeline.CompareOp.Equal => Silk.NET.Vulkan.CompareOp.Equal,
        NacreCraft.Client.Blaze3d.Pipeline.CompareOp.LessOrEqual => Silk.NET.Vulkan.CompareOp.LessOrEqual,
        NacreCraft.Client.Blaze3d.Pipeline.CompareOp.Greater => Silk.NET.Vulkan.CompareOp.Greater,
        NacreCraft.Client.Blaze3d.Pipeline.CompareOp.NotEqual => Silk.NET.Vulkan.CompareOp.NotEqual,
        NacreCraft.Client.Blaze3d.Pipeline.CompareOp.GreaterOrEqual => Silk.NET.Vulkan.CompareOp.GreaterOrEqual,
        NacreCraft.Client.Blaze3d.Pipeline.CompareOp.Always => Silk.NET.Vulkan.CompareOp.Always,
        _ => Silk.NET.Vulkan.CompareOp.Less
    };

    public override void Dispose()
    {
        if (_disposed) return;
        _vk.DestroyPipeline(_device, _pipeline, null);
        _vk.DestroyPipelineLayout(_device, _pipelineLayout, null);
        //4.3 rework removes the traditional RenderPass and no longer DestroyRenderPass
        _vk.DestroyShaderModule(_device, _fragModule, null);
        _vk.DestroyShaderModule(_device, _vertModule, null);
        _disposed = true;
    }
}
