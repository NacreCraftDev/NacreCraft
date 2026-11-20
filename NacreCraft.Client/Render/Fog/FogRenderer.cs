using System.Numerics;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Level;
using NacreCraft.Client.Render;

namespace NacreCraft.Client.Render.Fog;

//FogRenderer owns the fog UBO ring and computes per-frame fog distances, maps to vanilla net.minecraft.client.renderer.fog.FogRenderer
//NC has no fog-environment table yet, so the color and distances use the atmospheric defaults; the buffer layout is vanilla
public sealed class FogRenderer : IDisposable
{
    public const int FOG_UBO_SIZE = 16 + 6 * 4;

    private static bool _fogEnabled = true;

    private readonly GpuBuffer _emptyBuffer;
    private readonly MappableRingBuffer _regularBuffer;
    private bool _disposed;

    public FogRenderer()
    {
        var device = RenderSystem.GetDevice();
        _regularBuffer = new MappableRingBuffer("Fog UBO", GpuBuffer.UsageMapWrite | GpuBuffer.UsageUniform, FOG_UBO_SIZE);
        _emptyBuffer = device.CreateBuffer("Empty fog", GpuBuffer.UsageUniform | GpuBuffer.UsageCopyDst, FOG_UBO_SIZE);
        _emptyBuffer.Upload<byte>(new byte[FOG_UBO_SIZE]);
        RenderSystem.SetShaderFog(GetBuffer(FogMode.None));
    }

    public void EndFrame() => _regularBuffer.Rotate();

    public GpuBufferSlice GetBuffer(FogMode mode)
        => !_fogEnabled || mode == FogMode.None
            ? _emptyBuffer.Slice(0, FOG_UBO_SIZE)
            : _regularBuffer.CurrentBuffer().Slice(0, FOG_UBO_SIZE);

    //SetupFog builds the atmospheric fog distances for this frame, maps to vanilla setupFog
    //The environment-specific color table is not ported; sky blue is used and darkened by darkenWorldAmount
    public FogData SetupFog(Camera camera, int renderDistanceInChunks, float darkenWorldAmount, ClientLevel? level = null)
    {
        var renderDistanceInBlocks = renderDistanceInChunks * 16.0f;
        var span = Math.Clamp(renderDistanceInBlocks / 10.0f, 4.0f, 64.0f);
        var color = new Vector4(0.65f, 0.75f, 1.0f, 1.0f);
        if (darkenWorldAmount > 0.0f)
            color = Vector4.Lerp(color, new Vector4(color.X * 0.7f, color.Y * 0.6f, color.Z * 0.6f, 1.0f), darkenWorldAmount);

        return new FogData
        {
            Color = color,
            EnvironmentalStart = renderDistanceInBlocks * 0.25f,
            EnvironmentalEnd = renderDistanceInBlocks,
            RenderDistanceStart = renderDistanceInBlocks - span,
            RenderDistanceEnd = renderDistanceInBlocks,
            SkyEnd = renderDistanceInBlocks,
            CloudEnd = renderDistanceInBlocks * 4.0f
        };
    }

    //UpdateBuffer uploads the fog block into the current ring slot
    public void UpdateBuffer(FogData fog)
    {
        using var view = _regularBuffer.CurrentBuffer().Map(false, true);
        var bytes = Std140Builder.OnStack(FOG_UBO_SIZE)
            .PutVec4(fog.Color)
            .PutFloat(fog.EnvironmentalStart)
            .PutFloat(fog.EnvironmentalEnd)
            .PutFloat(fog.RenderDistanceStart)
            .PutFloat(fog.RenderDistanceEnd)
            .PutFloat(fog.SkyEnd)
            .PutFloat(fog.CloudEnd)
            .Get();
        bytes.AsSpan(0, FOG_UBO_SIZE).CopyTo(view.Data);
    }

    public static bool ToggleFog() => _fogEnabled = !_fogEnabled;

    public void Dispose()
    {
        if (_disposed) return;
        _emptyBuffer.Dispose();
        _regularBuffer.Dispose();
        _disposed = true;
    }

    public enum FogMode
    {
        None,
        World
    }
}
