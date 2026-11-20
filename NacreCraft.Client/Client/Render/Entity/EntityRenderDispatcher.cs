using System.Numerics;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Logging;

namespace NacreCraft.Client.Render.Entity;

//EntityRenderDispatcher dispatches visible entities into RenderType batches, maps to vanilla EntityRenderDispatcher
//Preparation writes every entity into a MultiBufferSource keyed by the renderer's RenderType; the world pass then records
//one draw per render type through PreparedRenderType
public sealed class EntityRenderDispatcher : IDisposable
{
    private readonly Dictionary<string, EntityRenderer> _renderers = new();
    private readonly List<(EntityRenderState State, EntityRenderer Renderer)> _visible = new();
    private readonly EntityVertexBuilder _builder = new();
    private readonly GpuBufferPool _bufferPool;
    //_device used to create the RenderType batch buffers; Upload fills it on the first rendered frame because the
    //Vulkan app's device is not ready yet when the world renderer is constructed
    private GpuDevice? _device;
    private MultiBufferSource.BufferSource? _buffers;
    private int _vertexCount;
    private bool _loggedFirstDraw;
    private bool _loggedFirstPrepare;
    private bool _disposed;

    public int EntityVertexCount => _vertexCount;
    public int EntityIndexCount => _vertexCount / 4 * 6;
    public int VisibleEntityCount => _visible.Count;
    public bool HasContent => _vertexCount > 0;
    public int LastDrawCallCount { get; private set; }

    public EntityRenderDispatcher(GpuBufferPool bufferPool, GpuDevice? device = null)
    {
        _bufferPool = bufferPool;
        _device = device ?? bufferPool.Device;
    }

    public void Register(string entityTypeName, EntityRenderer renderer)
        => _renderers[entityTypeName] = renderer;

    public void ClearEntities() => _visible.Clear();

    public void AddEntity(string entityTypeName, EntityRenderState state)
    {
        if (_renderers.TryGetValue(entityTypeName, out var renderer))
            _visible.Add((state, renderer));
    }

    //Prepare writes every visible entity into the render-type batch, world-space so the projection carries the camera
    public void Prepare(Vector3 cameraPosition)
    {
        _buffers ??= _device is { } device ? new MultiBufferSource.BufferSource(device) : null;
        if (_buffers is null) return;

        _buffers.Discard();
        _vertexCount = 0;
        foreach (var (state, renderer) in _visible)
        {
            _builder.Begin(_buffers.GetBuffer(renderer.RenderType(state)));
            var poseStack = new PoseStack();
            poseStack.Translate(state.Position.X, state.Position.Y, state.Position.Z);
            if (state.YRot != 0f)
                poseStack.Rotate(Quaternion.CreateFromAxisAngle(Vector3.UnitY, state.YRot));
            renderer.Render(poseStack, _builder, state);
            _vertexCount += _builder.VertexCount;
        }

        _builder.Clear();
        if (!_loggedFirstPrepare)
        {
            _loggedFirstPrepare = true;
            Log.Info($"Entity prepare visible={_visible.Count} vertices={_vertexCount} device={_device is not null}");
        }
    }

    //Upload records the render device; the staged buffers upload lazily in EndBatch
    public void Upload(GpuDevice device)
    {
        _device ??= device;
    }

    //Draw records the prepared batches into the open world pass
    public void Draw(RenderPass pass, Action<RenderPass> bindGlobals)
    {
        if (_buffers is null || _vertexCount == 0)
        {
            LastDrawCallCount = 0;
            return;
        }

        _buffers.EndBatch(pass);
        LastDrawCallCount = _buffers.LastDrawCount;
        if (!_loggedFirstDraw && LastDrawCallCount > 0)
        {
            _loggedFirstDraw = true;
            Log.Info($"Entity RenderType batches: draws={LastDrawCallCount} vertices={_vertexCount}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _buffers?.Dispose();
        _buffers = null;
        _disposed = true;
    }
}
