using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;

namespace NacreCraft.Client.Blaze3d.Pipeline;

//RenderTarget offscreen render target with a color texture and optional depth texture, maps to vanilla com.mojang.blaze3d.pipeline.RenderTarget
//Vanilla reaches the GPU through RenderSystem; NacreCraft has no global render state, so the device is held by the target itself
public abstract class RenderTarget
{
    private static int s_unnamedRenderTargets;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Label { get; }
    public bool UseDepth { get; }
    protected GpuFormat Format { get; }
    protected GpuDevice Device { get; }

    public GpuTexture? ColorTexture { get; private set; }
    public GpuTextureView? ColorTextureView { get; private set; }
    public GpuTexture? DepthTexture { get; private set; }
    public GpuTextureView? DepthTextureView { get; private set; }

    protected RenderTarget(GpuDevice device, string? label, bool useDepth, GpuFormat format)
    {
        Device = device;
        Label = label ?? "FBO " + s_unnamedRenderTargets++;
        UseDepth = useDepth;
        Format = format;
    }

    //Resize recreates both buffers at the new size
    public void Resize(int width, int height)
    {
        DestroyBuffers();
        CreateBuffers(width, height);
    }

    //DestroyBuffers frees the textures and views, the target becomes unusable until CreateBuffers runs again
    public void DestroyBuffers()
    {
        DepthTexture?.Dispose();
        DepthTexture = null;
        DepthTextureView?.Dispose();
        DepthTextureView = null;
        ColorTexture?.Dispose();
        ColorTexture = null;
        ColorTextureView?.Dispose();
        ColorTextureView = null;
    }

    //CopyDepthFrom copies the source depth buffer into this target, both must have one
    public void CopyDepthFrom(RenderTarget source)
    {
        if (DepthTexture is null)
            throw new InvalidOperationException("Trying to copy depth texture to a RenderTarget without a depth texture");
        if (source.DepthTexture is null)
            throw new InvalidOperationException("Trying to copy depth texture from a RenderTarget without a depth texture");

        using var encoder = Device.CreateCommandEncoder();
        encoder.CopyTextureToTexture(source.DepthTexture, DepthTexture, 0, 0, 0, 0, 0, Width, Height);
    }

    //CreateBuffers allocates the attachments, texture usage 15 is copy source, copy destination, sampled and render attachment
    public void CreateBuffers(int width, int height)
    {
        var maxTextureSize = Device.GetDeviceInfo().Limits.MaxTextureSize;
        if (width <= 0 || width > maxTextureSize || height <= 0 || height > maxTextureSize)
            throw new ArgumentException($"Window {width}x{height} size out of bounds (max. size: {maxTextureSize})");

        const int usage = GpuTexture.UsageCopyDst | GpuTexture.UsageCopySrc
                          | GpuTexture.UsageTextureBinding | GpuTexture.UsageRenderAttachment;

        Width = width;
        Height = height;
        if (UseDepth)
        {
            DepthTexture = Device.CreateTexture(Label + " / Depth", usage, GpuFormat.D32Float, width, height, 1, 1);
            DepthTextureView = Device.CreateTextureView(DepthTexture);
        }
        ColorTexture = Device.CreateTexture(Label + " / Color", usage, Format, width, height, 1, 1);
        ColorTextureView = Device.CreateTextureView(ColorTexture);
    }
}
