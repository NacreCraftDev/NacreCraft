using NacreCraft.Client.Blaze3d.Pipeline;

namespace NacreCraft.Client.Blaze3d;

//TracyFrameCapture could blit the frame and hand pixels to Tracy, maps to vanilla com.mojang.blaze3d.TracyFrameCapture
//NacreCraft has no Tracy native, so the class keeps the capture/upload/end-frame API while doing nothing
public sealed class TracyFrameCapture : IDisposable
{
    public void Capture(RenderTarget captureTarget)
    {
    }

    public void Upload()
    {
    }

    public void EndFrame()
    {
    }

    public void Dispose()
    {
    }
}
