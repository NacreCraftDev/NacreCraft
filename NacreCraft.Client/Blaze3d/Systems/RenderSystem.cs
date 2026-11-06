using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;

namespace NacreCraft.Client.Blaze3d.Systems;

//RenderSystem process-wide render state, maps to vanilla com.mojang.blaze3d.systems.RenderSystem
//The current renderer still passes GpuDevice explicitly; this class exists for the vanilla API surface and for code
//ported verbatim from vanilla that reads the device, projection or shader uniforms
public static class RenderSystem
{
    public const double DefaultDepthClearValue = 0.0;
    public const int MinimumAtlasTextureSize = 1024;
    public const int ProjectionMatrixUboSize = 64;

    private static Thread? _renderThread;
    private static GpuDevice? _device;
    private static SamplerCache? _samplerCache;

    private static GpuBufferSlice? _shaderFog;
    private static GpuBufferSlice? _shaderLightDirections;
    private static GpuBufferSlice? _projectionMatrixBuffer;
    private static GpuBufferSlice? _savedProjectionMatrixBuffer;
    private static ProjectionType _projectionType = ProjectionType.Perspective;
    private static ProjectionType _savedProjectionType = ProjectionType.Perspective;
    private static GpuBuffer? _globalSettingsUniform;

    private static readonly ScissorState ScissorStateForRenderTypeDraws = new();
    private static readonly Queue<(System.Action Callback, GpuFence Fence)> PendingFences = new();

    public static GpuTextureView? OutputColorTextureOverride;
    public static GpuTextureView? OutputDepthTextureOverride;

    public static SamplerCache GetSamplerCache()
        => _samplerCache ?? throw new InvalidOperationException("Can't get the sampler cache before the renderer was initialized");

    public static void InitRenderThread()
    {
        if (_renderThread is not null)
            throw new InvalidOperationException("Could not initialize render thread");
        _renderThread = Thread.CurrentThread;
    }

    public static bool IsOnRenderThread() => Thread.CurrentThread == _renderThread;

    public static void AssertOnRenderThread()
    {
        if (!IsOnRenderThread())
            throw new InvalidOperationException("Rendersystem called from wrong thread");
    }

    public static void InitRenderer(GpuDevice device)
    {
        if (_device is not null)
            throw new InvalidOperationException("RenderSystem.DEVICE already initialized");
        _device = device;
        _samplerCache = new SamplerCache(device);
    }

    public static void ShutdownRenderer()
    {
        _samplerCache?.Dispose();
        _samplerCache = null;
        while (PendingFences.TryDequeue(out var task)) task.Fence.Dispose();
        _device?.Dispose();
        _device = null;
    }

    public static GpuDevice GetDevice()
        => _device ?? throw new InvalidOperationException("Can't getDevice() before it was initialized");

    public static GpuDevice? TryGetDevice() => _device;

    public static void SetShaderFog(GpuBufferSlice fog) => _shaderFog = fog;

    public static GpuBufferSlice? GetShaderFog() => _shaderFog;

    public static void SetShaderLights(GpuBufferSlice buffer) => _shaderLightDirections = buffer;

    public static GpuBufferSlice? GetShaderLights() => _shaderLightDirections;

    public static void EnableScissorForRenderTypeDraws(int x, int y, int width, int height)
        => ScissorStateForRenderTypeDraws.Enable(x, y, width, height);

    public static void DisableScissorForRenderTypeDraws() => ScissorStateForRenderTypeDraws.Disable();

    public static ScissorState GetScissorStateForRenderTypeDraws() => ScissorStateForRenderTypeDraws;

    public static void SetProjectionMatrix(GpuBufferSlice projectionMatrixBuffer, ProjectionType type)
    {
        AssertOnRenderThread();
        _projectionMatrixBuffer = projectionMatrixBuffer;
        _projectionType = type;
    }

    public static void BackupProjectionMatrix()
    {
        AssertOnRenderThread();
        _savedProjectionMatrixBuffer = _projectionMatrixBuffer;
        _savedProjectionType = _projectionType;
    }

    public static void RestoreProjectionMatrix()
    {
        AssertOnRenderThread();
        _projectionMatrixBuffer = _savedProjectionMatrixBuffer;
        _projectionType = _savedProjectionType;
    }

    public static GpuBufferSlice? GetProjectionMatrixBuffer()
    {
        AssertOnRenderThread();
        return _projectionMatrixBuffer;
    }

    public static ProjectionType GetProjectionType()
    {
        AssertOnRenderThread();
        return _projectionType;
    }

    public static void SetGlobalSettingsUniform(GpuBuffer buffer) => _globalSettingsUniform = buffer;

    public static GpuBuffer? GetGlobalSettingsUniform() => _globalSettingsUniform;

    public static void QueueFencedTask(System.Action task)
    {
        using var encoder = GetDevice().CreateCommandEncoder();
        PendingFences.Enqueue((task, encoder.CreateFence()));
    }

    public static void ExecutePendingTasks()
    {
        while (PendingFences.TryPeek(out var task))
        {
            if (!task.Fence.AwaitCompletion(0L)) return;
            try
            {
                task.Callback();
            }
            finally
            {
                task.Fence.Dispose();
                PendingFences.Dequeue();
            }
        }
    }

    //BindDefaultUniforms binds the projection, fog, global settings and light buffers a ported pipeline expects
    public static void BindDefaultUniforms(RenderPass renderPass)
    {
        var projectionMatrix = GetProjectionMatrixBuffer();
        if (projectionMatrix is not null) renderPass.SetUniform("Projection", projectionMatrix);

        var fog = GetShaderFog();
        if (fog is not null) renderPass.SetUniform("Fog", fog);

        var globalUniform = GetGlobalSettingsUniform();
        if (globalUniform is not null) renderPass.SetUniform("Globals", globalUniform);

        var shaderLights = GetShaderLights();
        if (shaderLights is not null) renderPass.SetUniform("Lighting", shaderLights);
    }
}
