using System.Numerics;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Render;

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
    private static DynamicUniforms? _dynamicUniforms;
    //_projectionUbo owns the 64-byte projection block uploaded by SetProjectionMatrix(Matrix4x4)
    private static GpuBuffer? _projectionUbo;
    //_modelViewMatrix the transform uploaded for the current draw; NC uses a single matrix rather than vanilla's Matrix4fStack
    private static Matrix4x4 _modelViewMatrix = Matrix4x4.Identity;

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
        _dynamicUniforms = new DynamicUniforms();
        _projectionUbo = device.CreateBuffer("Projection UBO", GpuBuffer.UsageUniform | GpuBuffer.UsageMapWrite, ProjectionMatrixUboSize);
    }

    public static void ShutdownRenderer()
    {
        DetachRenderer();
        _device?.Dispose();
        _device = null;
    }

    //DetachRenderer releases every RenderSystem-owned resource but keeps the device alive, for owners that created the
    //device themselves (the NC window app) and destroy it after RenderSystem lets go
    public static void DetachRenderer()
    {
        _dynamicUniforms?.Dispose();
        _dynamicUniforms = null;
        _samplerCache?.Dispose();
        _samplerCache = null;
        _projectionUbo?.Dispose();
        _projectionUbo = null;
        _projectionMatrixBuffer = null;
        _savedProjectionMatrixBuffer = null;
        _shaderFog = null;
        _shaderLightDirections = null;
        _globalSettingsUniform = null;
        _modelViewMatrix = Matrix4x4.Identity;
        while (PendingFences.TryDequeue(out var task)) task.Fence.Dispose();
        _device = null;
        _renderThread = null;
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

    //SetProjectionMatrix uploads a matrix into RenderSystem's own projection UBO, maps to vanilla setProjectionMatrix
    //The renderer calls this once per frame before drawing anything that uses MATRICES_PROJECTION
    public static void SetProjectionMatrix(Matrix4x4 projectionMatrix, ProjectionType type = ProjectionType.Perspective)
    {
        AssertOnRenderThread();
        var ubo = _projectionUbo ?? throw new InvalidOperationException("Can't set the projection matrix before the renderer was initialized");
        ubo.Upload<Matrix4x4>(new[] { projectionMatrix });
        _projectionMatrixBuffer = ubo.Slice();
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

    //GetDynamicUniforms returns the per-frame transform rings, maps to vanilla getDynamicUniforms
    public static DynamicUniforms GetDynamicUniforms()
        => _dynamicUniforms ?? throw new InvalidOperationException("Can't getDynamicUniforms() before the renderer was initialized");

    //ResetDynamicUniforms starts a new frame for the transform rings
    public static void ResetDynamicUniforms() => _dynamicUniforms?.Reset();

    //GetModelViewMatrixCopy returns the current model-view matrix, maps to vanilla getModelViewMatrixCopy
    public static Matrix4x4 GetModelViewMatrixCopy()
    {
        AssertOnRenderThread();
        return _modelViewMatrix;
    }

    //SetModelViewMatrix updates the matrix later draws upload; NC has no Matrix4fStack, the renderer feeds the final matrix
    public static void SetModelViewMatrix(Matrix4x4 matrix)
    {
        AssertOnRenderThread();
        _modelViewMatrix = matrix;
    }

    //ResetModelViewMatrix returns to identity at the start of a frame
    public static void ResetModelViewMatrix()
    {
        AssertOnRenderThread();
        _modelViewMatrix = Matrix4x4.Identity;
    }

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
