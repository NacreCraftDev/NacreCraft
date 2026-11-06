using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Systems;

namespace NacreCraft.Client.Blaze3d.Pipeline;

//MainTarget the window-sized render target, maps to vanilla com.mojang.blaze3d.pipeline.MainTarget
//A failed allocation falls back to the default 854x480 size instead of taking the window down
public sealed class MainTarget : RenderTarget
{
    public const int DefaultWidth = 854;
    public const int DefaultHeight = 480;

    public MainTarget(GpuDevice device, int desiredWidth, int desiredHeight)
        : base(device, "Main", true, GpuFormat.Rgba8Unorm)
    {
        var maxTextureSize = device.GetDeviceInfo().Limits.MaxTextureSize;
        var candidates = desiredWidth <= 0 || desiredWidth > maxTextureSize || desiredHeight <= 0 || desiredHeight > maxTextureSize
            ? new[] { (DefaultWidth, DefaultHeight) }
            : new[] { (desiredWidth, desiredHeight), (DefaultWidth, DefaultHeight) };

        GpuOutOfMemoryException? lastOutOfMemory = null;
        foreach (var (width, height) in candidates)
        {
            DestroyBuffers();
            try
            {
                CreateBuffers(width, height);
                return;
            }
            catch (GpuOutOfMemoryException e)
            {
                lastOutOfMemory = e;
            }
        }

        throw new GpuOutOfMemoryException(
            $"Unrecoverable GPU out of memory for the main target: {lastOutOfMemory?.Message ?? "no attachment could be allocated"}");
    }
}
