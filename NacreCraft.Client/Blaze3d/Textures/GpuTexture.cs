using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Systems;
namespace NacreCraft.Client.Blaze3d.Textures;

//GpuTexture GPU texture, maps to vanilla com.mojang.blaze3d.textures.GpuTexture
//Holds the format and dimensions; the backend subclass owns the native handle
public abstract class GpuTexture : IDisposable
{
    public const int UsageCopyDst = 1;
    public const int UsageCopySrc = 2;
    public const int UsageTextureBinding = 4;
    public const int UsageRenderAttachment = 8;
    public const int UsageCubemapCompatible = 16;

    private readonly int _width;
    private readonly int _height;

    public GpuFormat Format { get; }
    public int DepthOrLayers { get; }
    public int MipLevels { get; }
    public int Usage { get; }
    public string Label { get; }

    protected GpuTexture(int usage, string label, GpuFormat format, int width, int height, int depthOrLayers, int mipLevels)
    {
        Usage = usage;
        Label = label;
        Format = format;
        _width = width;
        _height = height;
        DepthOrLayers = depthOrLayers;
        MipLevels = mipLevels;
    }

    //GetWidth width at the given mip level, maps to vanilla getWidth
    public int GetWidth(int mipLevel) => _width >> mipLevel;

    //GetHeight height at the given mip level, maps to vanilla getHeight
    public int GetHeight(int mipLevel) => _height >> mipLevel;

    //Width width at mip 0, convenience used by the NacreCraft renderer
    public int Width => _width;

    //Height height at mip 0, convenience used by the NacreCraft renderer
    public int Height => _height;

    //IsClosed whether the texture has been closed, maps to vanilla isClosed
    public abstract bool IsClosed { get; }

    //Upload uploads pixel data, a NacreCraft extension; vanilla uploads through CommandEncoder.writeToTexture
    public abstract void Upload(ReadOnlySpan<byte> pixels);

    //UploadRegion uploads a pixel region into an existing atlas texture, maps to vanilla GlyphBitmap.upload(x,y,texture)
    public virtual void UploadRegion(int x, int y, int width, int height, ReadOnlySpan<byte> pixels)
        => throw new NotSupportedException("UploadRegion is not implemented in this backend");

    //PendingRegionUpload one queued atlas write, drained by the render thread
    public readonly record struct PendingRegionUpload(GpuTexture Texture, int X, int Y, int Width, int Height, byte[] Pixels);

    //PendingRegionUploads writes queued from a worker thread, drained by FlushPendingRegionUploads
    private static readonly List<PendingRegionUpload> PendingRegionUploads = new();
    private static readonly object PendingRegionUploadsLock = new();

    //QueueRegionUpload queues an atlas write instead of submitting it on the spot
    //Glyph baking is demand driven and runs on the tick thread, so one submit-and-wait per glyph costs tens of
    //milliseconds each (OFAP S4.555); the render thread drains the whole queue in a single submit before it records
    public static void QueueRegionUpload(GpuTexture texture, int x, int y, int width, int height, ReadOnlySpan<byte> pixels)
    {
        var upload = new PendingRegionUpload(texture, x, y, width, height, pixels.ToArray());
        lock (PendingRegionUploadsLock) PendingRegionUploads.Add(upload);
    }

    //FlushPendingRegionUploads drains the queue with one submit, call it on the render thread before recording
    public static void FlushPendingRegionUploads(GpuDevice device)
    {
        PendingRegionUpload[] queued;
        lock (PendingRegionUploadsLock)
        {
            if (PendingRegionUploads.Count == 0) return;
            queued = PendingRegionUploads.ToArray();
            PendingRegionUploads.Clear();
        }
        //Grouped per texture so one atlas still goes through a single staging set and a single command
        foreach (var group in queued.GroupBy(upload => upload.Texture))
            group.Key.UploadRegionBatch(device, group.ToArray());
    }

    //UploadRegionBatch uploads several regions of this texture; the default keeps the one-at-a-time behaviour
    internal virtual void UploadRegionBatch(GpuDevice device, PendingRegionUpload[] batch)
    {
        foreach (var upload in batch)
            UploadRegion(upload.X, upload.Y, upload.Width, upload.Height, upload.Pixels);
    }

    //Readback reads pixels back to CPU memory, a NacreCraft test helper; vanilla reads back through CommandEncoder.copyTextureToBuffer
    public virtual byte[] Readback()
        => throw new NotSupportedException("Readback is not implemented in this backend");

    public abstract void Dispose();
}
