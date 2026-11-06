using System.Numerics;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Systems;

namespace NacreCraft.Client.Blaze3d.Resource;

//RenderTargetDescriptor blueprint for an offscreen render target, maps to vanilla com.mojang.blaze3d.resource.RenderTargetDescriptor
//Vanilla reads the device from RenderSystem during prepare; NacreCraft carries it in the descriptor because there is no global render state
public sealed record RenderTargetDescriptor(
    GpuDevice Device, int Width, int Height, bool UseDepth, Vector4 ClearColor, GpuFormat Format)
    : IResourceDescriptor<RenderTarget>
{
    public RenderTarget Allocate() => new TextureTarget(Device, null, Width, Height, UseDepth, Format);

    //Prepare clears the reused target to the descriptor color, depth goes to zero
    public void Prepare(RenderTarget resource)
    {
        using var encoder = Device.CreateCommandEncoder();
        if (UseDepth)
            encoder.ClearColorAndDepthTextures(resource.ColorTexture!, ClearColor, resource.DepthTexture!, 0.0);
        else
            encoder.ClearColorTexture(resource.ColorTexture!, ClearColor);
    }

    public void Free(RenderTarget resource) => resource.DestroyBuffers();

    //CanUsePhysicalResource dimensions, depth and format must match; the clear color can differ because prepare overwrites it
    public bool CanUsePhysicalResource(IResourceDescriptor other)
        => other is RenderTargetDescriptor descriptor
           && Width == descriptor.Width
           && Height == descriptor.Height
           && UseDepth == descriptor.UseDepth
           && Format == descriptor.Format;
}
