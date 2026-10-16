using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Buffer = Silk.NET.Vulkan.Buffer;
using RenderPipeline = NacreCraft.Client.Blaze3d.Pipeline.RenderPipeline;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;

using CompiledRenderPipeline = NacreCraft.Client.Blaze3d.Pipeline.CompiledRenderPipeline;
using RenderPipelineDescription = NacreCraft.Client.Blaze3d.Pipeline.RenderPipelineDescription;
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
using NacreCraft.Logging;
namespace NacreCraft.Client.Blaze3d.Vulkan;

//VulkanDevice Vulkan logical device, aligns with vanilla com.mojang.blaze3d.vulkan.VulkanDevice
//Owns the VkDevice + queues + command pool + descriptor pool and implements GpuDeviceBackend
public sealed unsafe class VulkanDevice : GpuDeviceBackend
{
    private readonly VulkanBackend _backend;
    private readonly ShaderManager _shaderManager;
    private readonly Vk _vk;
    private Device _device;
    private Queue _graphicsQueue;
    private Queue _presentQueue;
    private uint _graphicsFamily;
    private uint _presentFamily;
    private CommandPool _commandPool;
    private DescriptorPool _descriptorPool;
    //Counts the traced one-time submits, keeping the diagnostic output bounded
    private int _oneTimeDiag;

    //Sequence of every queue operation on the graphics queue: vkQueueSubmit, vkQueueWaitIdle and
    //vkQueuePresentKHR must be externally synchronized, and the Tick thread's one-time uploads otherwise race
    //the render thread's frame submit, which hangs the driver
    internal readonly object QueueLock = new();
    private KhrSwapchain _swapchainExtension;
    //DynamicRenderingExt the VK_KHR_dynamic_rendering extension for VulkanRenderPass to call CmdBeginRenderingKHR
    private KhrDynamicRendering _dynamicRenderingExtension;
    private PhysicalDeviceMemoryProperties _memoryProperties;
    private readonly DeviceInfo _deviceInfo;
    //_supportedFeatures what the physical device reports, used both to request creation features and to fill DeviceInfo
    private readonly PhysicalDeviceFeatures _supportedFeatures;
    private bool _disposed;

    public Vk Api => _vk;
    public Device Device => _device;
    public Queue GraphicsQueue => _graphicsQueue;
    public Queue PresentQueue => _presentQueue;
    public uint GraphicsFamilyIndex => _graphicsFamily;
    public uint PresentFamilyIndex => _presentFamily;
    public CommandPool CommandPool => _commandPool;
    public KhrSwapchain SwapchainExtension => _swapchainExtension;
    //DynamicRenderingExt exposes the KHR_dynamic_rendering extension instance for RenderPass to call CmdBeginRenderingKHR/CmdEndRenderingKHR
    public KhrDynamicRendering DynamicRenderingExt => _dynamicRenderingExtension;
    public PhysicalDevice PhysicalDevice => _backend.PhysicalDevice;
    //PipelineCache declarative RenderPipeline → CompiledRenderPipeline compile cache avoiding recompilation
    public NacreCraft.Client.Blaze3d.Pipeline.PipelineCache PipelineCache { get; }
    public ShaderManager ShaderManager => _shaderManager;

    internal VulkanDevice(VulkanBackend backend, ShaderManager shaderManager, GpuDebugOptions debugOptions)
    {
        _backend = backend;
        _shaderManager = shaderManager;
        _vk = backend.Api;
        var indices = backend.FindQueueFamilies(backend.PhysicalDevice);
        //The feature set is queried before device creation, since a creation request may only ask for what the GPU has
        _vk.GetPhysicalDeviceFeatures(backend.PhysicalDevice, out var supportedFeatures);
        _supportedFeatures = supportedFeatures;
        CreateLogicalDevice(indices);
        _vk.CurrentDevice = _device;
        if (!_vk.TryGetDeviceExtension(backend.Instance, _device, out _swapchainExtension))
        {
            throw new NotSupportedException("The KHR_swapchain device extension is unavailable");
        }
        //KHR_dynamic_rendering is a Vulkan 1.3 core extension; it replaces the traditional RenderPass with CmdBeginRenderingKHR
        if (!_vk.TryGetDeviceExtension(backend.Instance, _device, out _dynamicRenderingExtension))
        {
            throw new NotSupportedException("The KHR_dynamic_rendering device extension is unavailable; Vulkan 1.3+ or the KHR extension is required");
        }
        CreateCommandPool(indices);
        CreateDescriptorPool();
        _vk.GetPhysicalDeviceMemoryProperties(backend.PhysicalDevice, out _memoryProperties);
        _deviceInfo = BuildDeviceInfo();
        PipelineCache = new NacreCraft.Client.Blaze3d.Pipeline.PipelineCache();
    }

    public GpuSurfaceBackend CreateSurface(long windowHandle)
        => new VulkanGpuSurface(_backend, this, windowHandle);

    public CommandEncoderBackend CreateCommandEncoder()
        => new VulkanCommandEncoder(_vk, _device, this, _commandPool, _graphicsQueue, _dynamicRenderingExtension);

    public GpuSampler CreateSampler(AddressMode addressModeU, AddressMode addressModeV, FilterMode minFilter, FilterMode magFilter, int maxAnisotropy, double? maxLod)
        => new VulkanSampler(_vk, _device, addressModeU, addressModeV, minFilter, magFilter, maxAnisotropy, maxLod);

    public GpuTexture CreateTexture(string? label, int usage, GpuFormat format, int width, int height, int depthOrLayers, int mipLevels)
        => new VulkanImage(_vk, _device, this, usage, label ?? "", format, width, height, mipLevels);

    public GpuTextureView CreateTextureView(GpuTexture texture)
        => new VulkanTextureView((VulkanImage)texture, 0, texture.MipLevels);

    public GpuTextureView CreateTextureView(GpuTexture texture, int baseMipLevel, int mipLevels)
        => new VulkanTextureView((VulkanImage)texture, baseMipLevel, mipLevels);

    public GpuBuffer CreateBuffer(string? label, int usage, long size)
        => new VulkanBuffer(_vk, _device, this, label, usage, size);

    public IReadOnlyList<string> GetLastDebugMessages() => Array.Empty<string>();

    public bool IsDebuggingEnabled => false;

    //PipelineHits/PipelineMisses cache counters for the perf acceptance check
    public int PipelineHits => PipelineCache.HitCount;
    public int PipelineMisses => PipelineCache.MissCount;

    public CompiledRenderPipeline PrecompilePipeline(RenderPipeline pipeline)
        => PipelineCache.Precompile(pipeline, CompilePipeline);

    public void ClearPipelineCache() => PipelineCache.Clear();

    //CompilePipeline compiles a declarative RenderPipeline without caching, maps to vanilla VulkanDevice.compilePipeline
    private CompiledRenderPipeline CompilePipeline(RenderPipeline pipeline)
    {
        var description = RenderPipelineDescription.FromDeclaration(pipeline, _shaderManager);
        foreach (var layoutDesc in description.DescriptorLayoutDescriptions)
            description.DescriptorLayouts.Add(CreateDescriptorLayout(layoutDesc));
        description.DescriptorLayoutDescriptions.Clear();
        return VulkanRenderPipeline.FromDescription(this, description);
    }

    //CreateDescriptorLayout creates a VkDescriptorSetLayout, used internally while compiling pipelines
    internal GpuDescriptorLayout CreateDescriptorLayout(GpuDescriptorLayoutDescription description)
        => new VulkanDescriptorLayout(_vk, _device, description);

    //AllocateDescriptorSet allocates a VkDescriptorSet from the internal pool, used by render passes for name-based binding
    internal GpuDescriptorSet AllocateDescriptorSet(GpuDescriptorLayout layout)
    {
        var vkLayout = (VulkanDescriptorLayout)layout;
        var layouts = stackalloc DescriptorSetLayout[1];
        layouts[0] = vkLayout.Handle;
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = layouts
        };
        DescriptorSet set;
        if (_vk.AllocateDescriptorSets(_device, &allocInfo, &set) != Result.Success)
            throw new InvalidOperationException("DescriptorSet allocation failed");
        return new VulkanDescriptorSet(_vk, _device, vkLayout, set);
    }

    public GpuQueryPool CreateTimestampQueryPool(int size)
        => throw new NotSupportedException("Timestamp queries are not implemented yet");

    public long GetTimestampNow() => 0;

    public DeviceInfo GetDeviceInfo() => _deviceInfo;

    //FindMemoryType finds the memory type index matching typeBits and properties
    public uint FindMemoryType(uint typeBits, MemoryPropertyFlags properties)
    {
        for (int i = 0; i < _memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeBits & (1u << i)) != 0 &&
                (_memoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }
        throw new InvalidOperationException("No matching memory type found");
    }

    //CreateBufferInternal internally creates a native VkBuffer+DeviceMemory reused by VulkanBuffer/VulkanImage
    internal (Silk.NET.Vulkan.Buffer handle, DeviceMemory memory) CreateBufferInternal(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };
        Buffer buffer;
        if (_vk.CreateBuffer(_device, &bufferInfo, null, &buffer) != Result.Success)
            throw new InvalidOperationException("Buffer creation failed");
        _vk.GetBufferMemoryRequirements(_device, buffer, out var memRequirements);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };
        DeviceMemory memory;
        if (_vk.AllocateMemory(_device, &allocInfo, null, &memory) != Result.Success)
            throw new InvalidOperationException("Memory allocation failed");
        _vk.BindBufferMemory(_device, buffer, memory, 0);
        return (buffer, memory);
    }

    //RunOneTimeCommand submits a one-time command buffer and waits for completion
    //Used for image layout transitions and buffer copies
    public void RunOneTimeCommand(Action<CommandBuffer> record)
    {
        //Diagnostic: this submit shares the graphics queue and the command pool with the render thread's frame
        //submit, so a stall inside it only shows up as an enter without a matching leave
        var diag = Interlocked.Increment(ref _oneTimeDiag) <= 12;
        if (diag)
            Log.Info($"one-time command enter thread={Environment.CurrentManagedThreadId}");
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };
        CommandBuffer cmd;
        _vk.AllocateCommandBuffers(_device, &allocInfo, &cmd);
        var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };
        _vk.BeginCommandBuffer(cmd, &beginInfo);
        try
        {
            record(cmd);
        }
        finally
        {
            _vk.EndCommandBuffer(cmd);
        }
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd
        };
        lock (QueueLock)
        {
            _vk.QueueSubmit(_graphicsQueue, 1, &submitInfo, default);
            _vk.QueueWaitIdle(_graphicsQueue);
        }
        _vk.FreeCommandBuffers(_device, _commandPool, 1, &cmd);
        if (diag)
            Log.Info($"one-time command leave thread={Environment.CurrentManagedThreadId}");
    }
    //WaitIdle waits for all device queues to idle
    public void WaitIdle()
    {
        _vk.DeviceWaitIdle(_device);
    }

    private DeviceInfo BuildDeviceInfo()
    {
        //maxMemoryAllocationSize lives in VkPhysicalDeviceVulkan11Properties, chained through Properties2
        var props11 = new PhysicalDeviceVulkan11Properties { SType = StructureType.PhysicalDeviceVulkan11Properties };
        var props2 = new PhysicalDeviceProperties2 { SType = StructureType.PhysicalDeviceProperties2, PNext = &props11 };
        _vk.GetPhysicalDeviceProperties2(_backend.PhysicalDevice, &props2);
        var props = props2.Properties;
        var limits = props.Limits;

        var deviceLimits = new DeviceLimits(
            (int)limits.MaxSamplerAnisotropy,
            (int)limits.MinUniformBufferOffsetAlignment,
            (int)limits.MaxImageDimension2D,
            (long)props11.MaxMemoryAllocationSize,
            (int)limits.MaxDrawIndirectCount,
            (int)limits.MaxColorAttachments);

        //Indirect draw flags mirror what the device reports; drawIndirect is core 1.0, the multi-draw extension pairs stay off
        var features = new DeviceFeatures(
            ShaderDrawParameters: false,
            MultiDrawDirectInterleaved: false,
            MultiDrawDirectSeparate: false,
            MultiDrawIndirect: _supportedFeatures.MultiDrawIndirect,
            DrawIndirect: true,
            NonZeroFirstInstance: _supportedFeatures.DrawIndirectFirstInstance,
            PersistentMapping: false);
        var hints = new HintsAndWorkarounds(false, false);
        var extensions = new HashSet<string> { KhrSwapchain.ExtensionName, KhrDynamicRendering.ExtensionName };

        return new DeviceInfo(
            SilkMarshal.PtrToString((nint)props.DeviceName) ?? "Unknown",
            $"0x{props.VendorID:X}",
            $"0x{props.DriverVersion:X}",
            true,
            "Vulkan",
            limits.TimestampPeriod,
            deviceLimits,
            features,
            extensions,
            hints,
            ToDeviceType(props.DeviceType));
    }

    private static DeviceType ToDeviceType(PhysicalDeviceType type) => type switch
    {
        PhysicalDeviceType.IntegratedGpu => DeviceType.Integrated,
        PhysicalDeviceType.DiscreteGpu => DeviceType.Discrete,
        PhysicalDeviceType.VirtualGpu => DeviceType.Virtual,
        PhysicalDeviceType.Cpu => DeviceType.Cpu,
        _ => DeviceType.Other
    };

    private void CreateLogicalDevice(QueueFamilyIndices indices)
    {
        var uniqueQueueFamilies = indices.GraphicsFamily.Value == indices.PresentFamily.Value
            ? new[] { indices.GraphicsFamily.Value }
            : new[] { indices.GraphicsFamily.Value, indices.PresentFamily.Value };
        var queueCreateInfos = stackalloc DeviceQueueCreateInfo[uniqueQueueFamilies.Length];
        float queuePriority = 1f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }
        //Only the optional indirect draw features are requested; drawIndirect itself is core 1.0 and always on
        var deviceFeatures = new PhysicalDeviceFeatures
        {
            MultiDrawIndirect = _supportedFeatures.MultiDrawIndirect,
            DrawIndirectFirstInstance = _supportedFeatures.DrawIndirectFirstInstance
        };
        //KHR_swapchain swapchain + KHR_dynamic_rendering replacing the traditional RenderPass
        string[] deviceExtensions = { KhrSwapchain.ExtensionName, KhrDynamicRendering.ExtensionName };
        var enabledExtNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions);
        //PhysicalDeviceDynamicRenderingFeaturesKHR enables the dynamic rendering feature via the PNext chain
        var dynamicRenderingFeatures = new PhysicalDeviceDynamicRenderingFeaturesKHR
        {
            SType = StructureType.PhysicalDeviceDynamicRenderingFeatures,
            DynamicRendering = Vk.True
        };
        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            PNext = &dynamicRenderingFeatures,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,
            PEnabledFeatures = &deviceFeatures,
            EnabledExtensionCount = (uint)deviceExtensions.Length,
            PpEnabledExtensionNames = enabledExtNames
        };
        Device device;
        if (_vk.CreateDevice(_backend.PhysicalDevice, &createInfo, null, &device) != Result.Success)
        {
            throw new InvalidOperationException("VkDevice creation failed");
        }
        _device = device;
        _graphicsFamily = indices.GraphicsFamily.Value;
        _presentFamily = indices.PresentFamily.Value;
        _vk.GetDeviceQueue(_device, _graphicsFamily, 0, out _graphicsQueue);
        _vk.GetDeviceQueue(_device, _presentFamily, 0, out _presentQueue);
        SilkMarshal.Free((nint)enabledExtNames);
    }

    private void CreateCommandPool(QueueFamilyIndices indices)
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = indices.GraphicsFamily.Value,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };
        CommandPool pool;
        if (_vk.CreateCommandPool(_device, &poolInfo, null, &pool) != Result.Success)
        {
            throw new InvalidOperationException("Command pool creation failed");
        }
        _commandPool = pool;
    }

    //CreateDescriptorPool creates a descriptor pool with 100 uniform buffers and 100 combined image samplers
    private void CreateDescriptorPool()
    {
        var poolSizes = new[]
        {
            new DescriptorPoolSize { Type = DescriptorType.UniformBuffer, DescriptorCount = 100 },
            new DescriptorPoolSize { Type = DescriptorType.CombinedImageSampler, DescriptorCount = 100 }
        };
        fixed (DescriptorPoolSize* p = poolSizes)
        {
            var poolInfo = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = p,
                MaxSets = 100,
                Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit
            };
            if (_vk.CreateDescriptorPool(_device, &poolInfo, null, out _descriptorPool) != Result.Success)
                throw new InvalidOperationException("DescriptorPool creation failed");
        }
    }

    //ResetDescriptorPool frees every descriptor set handed out so far, maps to vanilla frame-scoped descriptor recycling
    //Sets are allocated per pipeline bind and never returned one by one, so the pool has to be reset periodically
    public void ResetDescriptorPool()
    {
        if (_descriptorPool.Handle != 0)
            _vk.ResetDescriptorPool(_device, _descriptorPool, 0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _vk.DestroyDescriptorPool(_device, _descriptorPool, null);
        _vk.DestroyCommandPool(_device, _commandPool, null);
        _vk.DestroyDevice(_device, null);
        _swapchainExtension?.Dispose();
        _dynamicRenderingExtension?.Dispose();
        _disposed = true;
    }
}
