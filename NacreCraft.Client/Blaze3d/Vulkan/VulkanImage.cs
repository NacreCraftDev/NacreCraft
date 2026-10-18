using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using VkFormat = Silk.NET.Vulkan.Format;
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

//VulkanImage Vulkan backend GPU image/texture
//Wraps VkImage + VkDeviceMemory + VkImageView
//Upload goes through a staging buffer + CmdCopyBufferToImage + a layout transition to ShaderReadOnlyOptimal
public sealed unsafe class VulkanImage : GpuTexture
{
    private readonly Vk _vk;
    private readonly Device _device;
    private readonly VulkanDevice _gpuDevice;
    private Image _image;
    private DeviceMemory _memory;
    private ImageView _view;
    private bool _disposed;
    //_currentLayout tracks the current image layout for repeated region-wise UploadRegion writes
    //Upload first goes Undefined→TransferDst→ShaderReadOnly; later UploadRegion goes ShaderReadOnly→TransferDst→ShaderReadOnly
    private ImageLayout _currentLayout = ImageLayout.Undefined;

    public Image Handle => _image;
    public ImageView View => _view;
    //CurrentLayout the current image layout for the blur flow's barrier decisions
    public ImageLayout CurrentLayout => _currentLayout;

    //IsClosed whether the image has been closed, maps to vanilla isClosed
    public override bool IsClosed => _disposed;

    internal VulkanImage(Vk vk, Device device, VulkanDevice gpuDevice,
        int usage, string label, GpuFormat format, int width, int height, int mipLevels)
        : base(usage, label, format, width, height, 1, mipLevels)
    {
        _vk = vk;
        _device = device;
        _gpuDevice = gpuDevice;
        var fmt = ToVkFormat(format);
        var vkUsage = ToVkUsage(usage, format);
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = fmt,
            Extent = new Extent3D { Width = (uint)Width, Height = (uint)Height, Depth = 1 },
            MipLevels = (uint)MipLevels,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = vkUsage,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined
        };
        if (_vk.CreateImage(_device, &imageInfo, null, out _image) != Result.Success)
            throw new InvalidOperationException("Image creation failed");
        _vk.GetImageMemoryRequirements(_device, _image, out var memReqs);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = gpuDevice.FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };
        if (_vk.AllocateMemory(_device, &allocInfo, null, out _memory) != Result.Success)
            throw new InvalidOperationException("Image memory allocation failed");
        _vk.BindImageMemory(_device, _image, _memory, 0);
        CreateView();
        //ColorAttachment initial layout transition Undefined->ColorAttachmentOptimal
        //dynamic rendering expects ColorAttachmentOptimal; without the transition written data is lost or a validation warning appears
        //A SampledImage without ColorAttachment uses the Upload path transition; a DepthAttachment uses the Upload(Empty) transition
        //ColorAttachment|SampledImage (e.g. AtlasTexture) needs ColorAttachmentOptimal before the first render
        if ((usage & GpuTexture.UsageRenderAttachment) != 0 && !format.HasDepthAspect())
            TransitionInitialColorAttachment();
    }

    //TransitionInitialColorAttachment transitions a ColorAttachment image from Undefined to ColorAttachmentOptimal
    //For color attachments like AtlasTexture that upload no pixels, this must be in place before dynamic rendering
    private void TransitionInitialColorAttachment()
    {
        _gpuDevice.RunOneTimeCommand(cmd =>
        {
            TransitionLayout(cmd, ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal,
                AccessFlags.None, AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit,
                PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.ColorAttachmentOutputBit,
                ImageAspectFlags.ColorBit);
        });
        _currentLayout = ImageLayout.ColorAttachmentOptimal;
    }

    private void CreateView()
    {
        var fmt = ToVkFormat(Format);
        //DepthAttachment uses the DepthBit aspect, others use ColorBit
        var aspectMask = Format.HasDepthAspect()
            ? ImageAspectFlags.DepthBit
            : ImageAspectFlags.ColorBit;
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _image,
            ViewType = ImageViewType.Type2D,
            Format = fmt,
            Components = default,
            SubresourceRange =
            {
                AspectMask = aspectMask,
                BaseMipLevel = 0,
                LevelCount = (uint)MipLevels,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
        if (_vk.CreateImageView(_device, &viewInfo, null, out _view) != Result.Success)
            throw new InvalidOperationException("ImageView creation failed");
    }

    //Upload copies into the device-local image via a staging buffer then transitions the layout to ShaderReadOnlyOptimal
    //DepthAttachment uploads no pixels and only does the Undefined->DepthStencilAttachmentOptimal layout transition
    public override void Upload(ReadOnlySpan<byte> pixels)
    {
        if (Format.HasDepthAspect())
        {
            _gpuDevice.RunOneTimeCommand(cmd =>
            {
                TransitionLayout(cmd, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal,
                    AccessFlags.None, AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit,
                    PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.EarlyFragmentTestsBit,
                    ImageAspectFlags.DepthBit);
            });
            _currentLayout = ImageLayout.DepthStencilAttachmentOptimal;
            return;
        }
        var staging = (VulkanBuffer)_gpuDevice.CreateBuffer(null, GpuBuffer.UsageCopySrc | GpuBuffer.UsageCopyDst | GpuBuffer.UsageMapWrite, pixels.Length);
        try
        {
            staging.Upload(pixels.ToArray());

            _gpuDevice.RunOneTimeCommand(cmd =>
            {
                TransitionLayout(cmd, _currentLayout, ImageLayout.TransferDstOptimal,
                    AccessFlags.None, AccessFlags.TransferWriteBit,
                    PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.TransferBit);

                var region = new BufferImageCopy
                {
                    BufferOffset = 0,
                    BufferRowLength = 0,
                    BufferImageHeight = 0,
                    ImageSubresource =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        MipLevel = 0,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    },
                    ImageOffset = default,
                    ImageExtent = new Extent3D { Width = (uint)Width, Height = (uint)Height, Depth = 1 }
                };
                fixed (Image* img = &_image)
                {
                    _vk.CmdCopyBufferToImage(cmd, staging.Handle, _image, ImageLayout.TransferDstOptimal, 1, &region);
                }

                TransitionLayout(cmd, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal,
                    AccessFlags.TransferWriteBit, AccessFlags.ShaderReadBit,
                    PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit);
            });
            _currentLayout = ImageLayout.ShaderReadOnlyOptimal;
        }
        finally
        {
            staging.Dispose();
        }
    }

    //UploadRegion uploads pixels region-wise into an atlas sub-region, corresponds to vanilla GlyphBitmap.upload(x,y,texture)
    //Used by dynamic glyph baking; the FontTexture 256×256 atlas writes new glyph pixels on demand
    //ShaderReadOnly→TransferDst→write region→ShaderReadOnly full layout transition cycle
    public override void UploadRegion(int x, int y, int width, int height, ReadOnlySpan<byte> pixels)
    {
        if (Format.HasDepthAspect())
            throw new InvalidOperationException("DepthAttachment does not support UploadRegion");
        var staging = (VulkanBuffer)_gpuDevice.CreateBuffer(null, GpuBuffer.UsageCopySrc | GpuBuffer.UsageCopyDst | GpuBuffer.UsageMapWrite, pixels.Length);
        try
        {
            staging.Upload(pixels.ToArray());

            _gpuDevice.RunOneTimeCommand(cmd =>
            {
                //The source access and stage follow the tracked layout: the first region upload starts from Undefined,
                //where the source access mask has to be empty and the source stage is TopOfPipe
                var (srcAccess, srcStage) = GetBarrierParams(_currentLayout);
                TransitionLayout(cmd, _currentLayout, ImageLayout.TransferDstOptimal,
                    srcAccess, AccessFlags.TransferWriteBit,
                    srcStage, PipelineStageFlags.TransferBit);

                var region = new BufferImageCopy
                {
                    BufferOffset = 0,
                    BufferRowLength = (uint)width,
                    BufferImageHeight = (uint)height,
                    ImageSubresource =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        MipLevel = 0,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    },
                    ImageOffset = new Offset3D { X = x, Y = y, Z = 0 },
                    ImageExtent = new Extent3D { Width = (uint)width, Height = (uint)height, Depth = 1 }
                };
                fixed (Image* img = &_image)
                {
                    _vk.CmdCopyBufferToImage(cmd, staging.Handle, _image, ImageLayout.TransferDstOptimal, 1, &region);
                }

                TransitionLayout(cmd, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal,
                    AccessFlags.TransferWriteBit, AccessFlags.ShaderReadBit,
                    PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit);
            });
            _currentLayout = ImageLayout.ShaderReadOnlyOptimal;
        }
        finally
        {
            staging.Dispose();
        }
    }

    //Staging buffers from the previous batch; its submit was not waited on, so they are freed on the next flush,
    //which is a frame later and well past the point the queue consumed them
    private readonly List<VulkanBuffer> _retiredStagings = new();

    //UploadRegionBatch uploads several regions in one submit
    //Each region still gets its own staging buffer, but the layout transition, the copies and the transition back all
    //land in a single command, which turns N submit-and-waits into one
    internal override void UploadRegionBatch(GpuDevice device, GpuTexture.PendingRegionUpload[] batch)
    {
        if (Format.HasDepthAspect())
            throw new InvalidOperationException("DepthAttachment does not support UploadRegion");
        foreach (var retired in _retiredStagings) retired.Dispose();
        _retiredStagings.Clear();
        var stagings = new List<VulkanBuffer>(batch.Length);
        try
        {
            foreach (var upload in batch)
            {
                var staging = (VulkanBuffer)_gpuDevice.CreateBuffer(null,
                    GpuBuffer.UsageCopySrc | GpuBuffer.UsageCopyDst | GpuBuffer.UsageMapWrite, upload.Pixels.Length);
                staging.Upload(upload.Pixels);
                stagings.Add(staging);
            }

            _gpuDevice.RunOneTimeCommand(cmd =>
            {
                //The source access and stage follow the tracked layout: the first region upload starts from Undefined,
                //where the source access mask has to be empty and the source stage is TopOfPipe
                var (srcAccess, srcStage) = GetBarrierParams(_currentLayout);
                TransitionLayout(cmd, _currentLayout, ImageLayout.TransferDstOptimal,
                    srcAccess, AccessFlags.TransferWriteBit,
                    srcStage, PipelineStageFlags.TransferBit);

                for (int i = 0; i < batch.Length; i++)
                {
                    var upload = batch[i];
                    var region = new BufferImageCopy
                    {
                        BufferOffset = 0,
                        BufferRowLength = (uint)upload.Width,
                        BufferImageHeight = (uint)upload.Height,
                        ImageSubresource =
                        {
                            AspectMask = ImageAspectFlags.ColorBit,
                            MipLevel = 0,
                            BaseArrayLayer = 0,
                            LayerCount = 1
                        },
                        ImageOffset = new Offset3D { X = upload.X, Y = upload.Y, Z = 0 },
                        ImageExtent = new Extent3D { Width = (uint)upload.Width, Height = (uint)upload.Height, Depth = 1 }
                    };
                    fixed (Image* img = &_image)
                    {
                        _vk.CmdCopyBufferToImage(cmd, stagings[i].Handle, _image, ImageLayout.TransferDstOptimal, 1, &region);
                    }
                }

                TransitionLayout(cmd, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal,
                    AccessFlags.TransferWriteBit, AccessFlags.ShaderReadBit,
                    PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit);
            }, waitForCompletion: false);
            _currentLayout = ImageLayout.ShaderReadOnlyOptimal;
        }
        finally
        {
            //Handed to the queue but not waited on, so ownership moves to the next flush instead of freeing here
            _retiredStagings.AddRange(stagings);
        }
    }

    //TransitionLayout layout transition helper; aspect defaults to ColorBit, depth images pass DepthBit
    private void TransitionLayout(CommandBuffer cmd, ImageLayout oldLayout, ImageLayout newLayout,
        AccessFlags srcAccess, AccessFlags dstAccess, PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        ImageAspectFlags aspect = ImageAspectFlags.ColorBit)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = _image,
            SubresourceRange =
            {
                AspectMask = aspect,
                BaseMipLevel = 0,
                LevelCount = (uint)MipLevels,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess
        };
        _vk.CmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrier);
    }

    //TransitionLayout transitions the image from _currentLayout to newLayout, recorded into an external cmd buffer
    //blur offscreen chain Undefined→ColorAttachmentOptimal→ShaderReadOnlyOptimal→ColorAttachmentOptimal
    //The access mask and stage are inferred from the layout so the caller need not fill them manually
    public void TransitionLayout(CommandBuffer cmd, ImageLayout newLayout)
    {
        if (_currentLayout == newLayout) return;
        var aspect = Format.HasDepthAspect() ? ImageAspectFlags.DepthBit : ImageAspectFlags.ColorBit;
        var (srcAccess, srcStage) = GetBarrierParams(_currentLayout);
        var (dstAccess, dstStage) = GetBarrierParams(newLayout);
        TransitionLayout(cmd, _currentLayout, newLayout, srcAccess, dstAccess, srcStage, dstStage, aspect);
        _currentLayout = newLayout;
    }

    //GetBarrierParams infers the access mask and pipeline stage from the layout
    private static (AccessFlags, PipelineStageFlags) GetBarrierParams(ImageLayout layout) => layout switch
    {
        ImageLayout.Undefined => (AccessFlags.None, PipelineStageFlags.TopOfPipeBit),
        ImageLayout.ColorAttachmentOptimal => (AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit, PipelineStageFlags.ColorAttachmentOutputBit),
        ImageLayout.ShaderReadOnlyOptimal => (AccessFlags.ShaderReadBit, PipelineStageFlags.FragmentShaderBit),
        ImageLayout.TransferDstOptimal => (AccessFlags.TransferWriteBit, PipelineStageFlags.TransferBit),
        ImageLayout.TransferSrcOptimal => (AccessFlags.TransferReadBit, PipelineStageFlags.TransferBit),
        ImageLayout.General => (AccessFlags.MemoryReadBit | AccessFlags.MemoryWriteBit, PipelineStageFlags.AllCommandsBit),
        _ => (AccessFlags.None, PipelineStageFlags.BottomOfPipeBit)
    };

    //Readback reads GPU image pixels back to the CPU for integration tests to verify render output
    //Flow current layout→TransferSrcOptimal→CmdCopyImageToBuffer→map staging buffer→read on CPU→restore the original layout
    //AtlasTexture is ColorAttachmentOptimal after rendering and is restored to ColorAttachmentOptimal after reading
    //DepthAttachment does not support readback; the PoC does not verify depth images
    public override byte[] Readback()
    {
        if (Format.HasDepthAspect())
            throw new NotSupportedException("DepthAttachment does not support Readback");
        var pixelSize = Width * Height * 4;
        var staging = (VulkanBuffer)_gpuDevice.CreateBuffer(null, GpuBuffer.UsageCopySrc | GpuBuffer.UsageCopyDst | GpuBuffer.UsageMapWrite, pixelSize);
        try
        {
            var originalLayout = _currentLayout;
            _gpuDevice.RunOneTimeCommand(cmd =>
            {
                //Current layout→TransferSrcOptimal
                TransitionLayout(cmd, _currentLayout, ImageLayout.TransferSrcOptimal,
                    GetBarrierParams(_currentLayout).Item1, AccessFlags.TransferReadBit,
                    GetBarrierParams(_currentLayout).Item2, PipelineStageFlags.TransferBit,
                    ImageAspectFlags.ColorBit);

                //CmdCopyImageToBuffer image→staging buffer
                var region = new BufferImageCopy
                {
                    BufferOffset = 0,
                    BufferRowLength = 0,
                    BufferImageHeight = 0,
                    ImageSubresource =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        MipLevel = 0,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    },
                    ImageOffset = default,
                    ImageExtent = new Extent3D { Width = (uint)Width, Height = (uint)Height, Depth = 1 }
                };
                fixed (Image* img = &_image)
                {
                    _vk.CmdCopyImageToBuffer(cmd, _image, ImageLayout.TransferSrcOptimal, staging.Handle, 1, &region);
                }

                //TransferSrcOptimal→original layout restored for subsequent rendering
                TransitionLayout(cmd, ImageLayout.TransferSrcOptimal, originalLayout,
                    AccessFlags.TransferReadBit, GetBarrierParams(originalLayout).Item1,
                    PipelineStageFlags.TransferBit, GetBarrierParams(originalLayout).Item2,
                    ImageAspectFlags.ColorBit);
            });
            _currentLayout = originalLayout;

            //map staging buffer read on CPU
            var pixels = new byte[pixelSize];
            staging.Download<byte>(pixels);
            return pixels;
        }
        finally
        {
            staging.Dispose();
        }
    }

    private static VkFormat ToVkFormat(GpuFormat fmt) => fmt switch
    {
        GpuFormat.Rgba8Unorm => VkFormat.R8G8B8A8Unorm,
        GpuFormat.Bgra8Unorm => VkFormat.B8G8R8A8Unorm,
        GpuFormat.Rgb8Unorm => VkFormat.R8G8B8Unorm,
        GpuFormat.R8Unorm => VkFormat.R8Unorm,
        GpuFormat.D32Float => VkFormat.D32Sfloat,
        _ => throw new ArgumentOutOfRangeException(nameof(fmt))
    };

    private static ImageUsageFlags ToVkUsage(int usage, GpuFormat format)
    {
        var flags = ImageUsageFlags.None;
        if ((usage & GpuTexture.UsageTextureBinding) != 0)
            flags |= ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit;
        if ((usage & GpuTexture.UsageRenderAttachment) != 0)
            flags |= format.HasDepthAspect() ? ImageUsageFlags.DepthStencilAttachmentBit : ImageUsageFlags.ColorAttachmentBit;
        //All color images allow TransferSrc so Readback can use CmdCopyImageToBuffer
        if (!format.HasDepthAspect())
            flags |= ImageUsageFlags.TransferSrcBit;
        return flags;
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _vk.DestroyImageView(_device, _view, null);
        _vk.DestroyImage(_device, _image, null);
        _vk.FreeMemory(_device, _memory, null);
        _disposed = true;
    }
}
