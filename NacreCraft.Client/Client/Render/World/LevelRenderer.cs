using System.Numerics;
using System.Runtime.InteropServices;
using NacreCraft.Client.Level;
using NacreCraft.Client.Render.Culling;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Render;
using NacreCraft.Primitives;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
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

namespace NacreCraft.Client.Render.World;

//LevelRenderer world rendering main entry, maps to vanilla LevelRenderer
//W8 rework: removed synchronous StagedVertexBuffer building, holds a SectionRenderDispatcher for async compiling; the Render thread only does Upload+Draw
//Prepare builds the frustum and calls dispatcher.SetCameraPosition to trigger a ViewArea diff, enqueueing newly visible sections for compile
//Upload calls dispatcher.UploadTerrainBuffers to upload compiled meshes inside the Lock
//Draw iterates EnumerateVisibleSections in Solid→Cutout→Translucent order and submits via GetSectionSlice
//One DrawCall per section: 100 sections = 300 DrawCalls; uber buffer batching left as an optimization
//Vertex format is the 20-byte compact chunk format; vertices are section-local and the section origin goes through a push constant
//Subscribes to level.SectionDirty and forwards to dispatcher.MarkDirty, spreading to itself + 6 neighbors
public sealed class LevelRenderer : IDisposable, IWorldRenderer
{
    private readonly ClientLevel _level;
    private readonly Camera _camera;
    private readonly SectionRenderDispatcher _dispatcher;
    private readonly Action<SectionPos> _sectionDirtyHandler;
    private readonly Action<SectionPos> _chunkUnloadedHandler;
    private readonly GpuBufferPool _bufferPool;
    //Visible section buffer reused across frames to avoid allocating a List every frame
    private readonly List<RenderSection> _visibleSectionBuffer = new();
    //Draw batch scratch reused every frame: the batches of the current layer plus the flat command list they index
    private readonly List<ChunkDrawBatch> _batches = new();
    private readonly List<IndirectDrawCommand> _commands = new();
    //_indirectCommands host-visible buffer holding this frame's batched indirect draw commands, grown on demand
    private GpuBuffer? _indirectCommands;
    private long _indirectCapacity;
    //_multiDrawIndirect whether the device accepts more than one command in a single DrawIndexedIndirect call
    private bool _multiDrawIndirect;

    //This frame's frustum; built in Prepare and reused by Draw
    private Frustum? _frustum;
    //This frame's total vertex count; accumulated in Draw for F3 display
    private int _totalVertexCount;
    //EntityDispatcher entity render dispatcher; null means no entity rendering, injected by the Game layer
    public EntityRenderDispatcher? EntityDispatcher { get; set; }

    //MovingBlocks moving block render pass; null means no rendering of advancing pistons, injected by the Game layer
    public MovingBlockRenderer? MovingBlocks { get; set; }

    //Performance metrics for GameScreen's F3 display
    public int SectionCount => _dispatcher.SectionCount;
    public int VisibleSectionCount => _dispatcher.VisibleSectionCount;
    public int TotalVertexCount => _totalVertexCount;
    public int DrawCallCount { get; private set; }

    //ViewProj this frame's view*proj matrix; VulkanGuiApp reads this property to upload the set 0 UBO
    public Matrix4x4 ViewProj => _camera.GetViewProjMatrix();

    public LevelRenderer(ClientLevel level, Camera camera, SectionRenderDispatcher dispatcher, GpuBufferPool bufferPool)
    {
        _level = level;
        _camera = camera;
        _dispatcher = dispatcher;
        _bufferPool = bufferPool;
        _sectionDirtyHandler = pos => _dispatcher.MarkDirty(pos);
        _level.SectionDirty += _sectionDirtyHandler;
        //On chunk unload, clean up RenderSection and release GPU buffers to prevent leaks
        //UnloadSection holds the Lock to prevent racing with the Render thread's Upload/Draw over buffer references
        _chunkUnloadedHandler = pos =>
        {
            _dispatcher.Lock();
            try { _dispatcher.UnloadSection(pos); }
            finally { _dispatcher.Unlock(); }
        };
        _level.ChunkUnloaded += _chunkUnloadedHandler;
    }

    //Prepare builds the frustum and calls dispatcher.SetCameraPosition to trigger a ViewArea diff, enqueueing newly visible sections for compile
    //W7 synchronous traversal Build removed; mesh building moved to the dispatcher's background thread
    public void Prepare()
    {
        _frustum = new Frustum(_camera.GetViewProjMatrix());
        _dispatcher.SetCameraPosition(_camera, _frustum);
        PrepareEntities();
        //Advancing pistons are re-baked every frame; the displacement changes with ticks and a static mesh cannot carry it
        MovingBlocks?.Prepare();
    }

    //PrepareEntities converts client entities into render states for the entity dispatcher
    //Types without a registered renderer are skipped by the dispatcher itself; no type filtering here
    private void PrepareEntities()
    {
        var entities = EntityDispatcher;
        if (entities is null) return;
        entities.ClearEntities();
        foreach (var entity in _level.Entities)
        {
            var typeName = entity.Type.Id.ToString();
            entities.AddEntity(typeName, new EntityRenderState
            {
                Position = new Vector3((float)entity.Pos.X, (float)entity.Pos.Y, (float)entity.Pos.Z),
                YRot = entity.YRot,
                XRot = entity.XRot,
                Name = typeName,
                //Age in ticks is derived from the client world tick; when the world is frozen the count does not advance, so animation stops
                AgeInTicks = _level.TickCount - entity.SpawnedAtTick,
                BobOffset = entity.BobOffset,
                //Business data the renderer needs is passed through here; the Gpu layer does not know entity types
                CustomData = entity.DroppedItem,
            });
        }
        entities.Prepare(_camera.Position);
    }

    //Upload calls dispatcher.UploadTerrainBuffers to upload compiled meshes inside the Lock into GpuBufferPool-borrowed buffers
    //The device parameter is kept for IWorldRenderer signature compatibility; the dispatcher internally uses the device bound to GpuBufferPool
    public void Upload(GpuDevice device)
    {
        //Cached once: whether a single indirect call may carry more than one command
        _multiDrawIndirect = device.GetDeviceInfo().Features.MultiDrawIndirect;
        _dispatcher.Lock();
        try { _dispatcher.UploadTerrainBuffers(); }
        finally { _dispatcher.Unlock(); }
        EntityDispatcher?.Upload(device);
        MovingBlocks?.Upload(device);
    }

    //Draw iterates visible sections in Solid→Cutout→Translucent order and submits via GetSectionSlice
    //Pipeline switches per layer: one SetPipeline+bindGlobals per layer, one DrawCall per layer within a section
    //Traversal inside the Lock ensures buffer references are not reclaimed between Upload and Draw
    public void Draw(RenderPass pass, Action<RenderPass> bindGlobals)
    {
        DrawCallCount = 0;
        _totalVertexCount = 0;
        if (_frustum is null) return;
        _dispatcher.Lock();
        try
        {
            _visibleSectionBuffer.Clear();
            var cameraPos = _camera.Position;
            foreach (var section in _dispatcher.EnumerateVisibleSections(_frustum))
            {
                var dx = cameraPos.X - ((section.Pos.X << 4) + 8);
                var dy = cameraPos.Y - ((section.Pos.Y << 4) + 8);
                var dz = cameraPos.Z - ((section.Pos.Z << 4) + 8);
                section.CameraDistanceSq = dx * dx + dy * dy + dz * dz;
                _visibleSectionBuffer.Add(section);
                if (section.Mesh is not null) _totalVertexCount += section.Mesh.TotalVertexCount;
            }
            //Sort by region so the region origin is pushed once per contiguous run instead of once per section
            _visibleSectionBuffer.Sort(static (a, b) => a.RegionKey.CompareTo(b.RegionKey));
            foreach (var layer in s_layers)
            {
                var pipeline = layer switch
                {
                    RenderLayer.Solid => WorldRenderPipelines.SOLID_TERRAIN,
                    RenderLayer.Cutout => WorldRenderPipelines.CUTOUT_TERRAIN,
                    RenderLayer.Translucent => WorldRenderPipelines.TRANSLUCENT_TERRAIN,
                    _ => throw new InvalidOperationException($"Unknown RenderLayer {layer}")
                };
                pass.SetPipeline(pipeline);
                bindGlobals(pass);
                //The terrain pipeline enables VK_DYNAMIC_STATE_SCISSOR, so CmdSetScissor must be called before draw or driver behavior is undefined
                pass.DisableScissor();
                DrawLayer(pass, layer);
            }
        }
        finally { _dispatcher.Unlock(); }
        //Moving blocks use the terrain pipelines and are drawn after chunks in the same pass
        if (MovingBlocks is not null)
        {
            MovingBlocks.Draw(pass, bindGlobals);
            DrawCallCount += MovingBlocks.LastDrawCallCount;
        }
        //Entity rendering comes after terrain in the same world pass; depth test ensures correct occlusion
        //Batched by model.Pipeline into Solid/Cutout/Translucent, each layer with its own SetPipeline
        if (EntityDispatcher is not null && EntityDispatcher.HasContent)
        {
            EntityDispatcher.Draw(pass, bindGlobals);
            DrawCallCount += EntityDispatcher.LastDrawCallCount;
        }
    }

    //DrawLayer collects the layer's visible sections into batches sharing one vertex block and one index block,
    //then submits each batch with a single DrawIndexedIndirect
    //Sections arrive grouped by region, and within a region the arena hands neighbouring sections neighbouring
    //ranges, so batches stay large
    private void DrawLayer(RenderPass pass, RenderLayer layer)
    {
        CollectBatches(layer);
        if (_batches.Count == 0) return;
        var commandBytes = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_commands));
        var commandBuffer = EnsureIndirectCapacity(commandBytes.Length);
        using (var view = commandBuffer.Map(0, commandBytes.Length, false, true))
            commandBytes.CopyTo(view.Data);

        foreach (var batch in _batches)
        {
            //The region origin is the only per-batch constant; the section slot baked into each vertex picks the rest
            var origin = new Vector4(batch.Region.Pos.X << 7, batch.Region.Pos.Y << 6, batch.Region.Pos.Z << 7, 0f);
            pass.SetPushConstant(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref origin, 1)));
            pass.SetVertexBuffer(0, batch.VertexBuffer.Slice());
            pass.SetIndexBuffer(batch.IndexBuffer, NacreCraft.Client.Blaze3d.IndexType.Int);
            if (_multiDrawIndirect && batch.CommandCount > 1)
            {
                pass.DrawIndexedIndirect(
                    commandBuffer.Slice((long)batch.FirstCommand * IndirectDrawCommand.Size, (long)batch.CommandCount * IndirectDrawCommand.Size),
                    batch.CommandCount);
                DrawCallCount++;
            }
            else
            {
                //Without multiDrawIndirect a batch still goes out as indirect commands, one call each
                for (var i = 0; i < batch.CommandCount; i++)
                {
                    pass.DrawIndexedIndirect(
                        commandBuffer.Slice((long)(batch.FirstCommand + i) * IndirectDrawCommand.Size, IndirectDrawCommand.Size), 1);
                    DrawCallCount++;
                }
            }
        }
    }

    //CollectBatches walks the sorted visible sections and groups consecutive ones that share both arena buffers
    private void CollectBatches(RenderLayer layer)
    {
        //Translucent surfaces must go back to front; reordering the commands is enough, since the vertex data
        //stays where it is and only the submission order changes
        if (layer == RenderLayer.Translucent)
            _visibleSectionBuffer.Sort(static (a, b) => b.CameraDistanceSq.CompareTo(a.CameraDistanceSq));
        _batches.Clear();
        _commands.Clear();
        ChunkDrawBatch? current = null;
        foreach (var section in _visibleSectionBuffer)
        {
            var slice = _dispatcher.GetSectionSlice(section.Pos, layer);
            if (slice is null) continue;
            if (section.Region is not { } region) continue;
            var value = slice.Value;
            if (current is null
                || !ReferenceEquals(current.Region, region)
                || !ReferenceEquals(current.VertexBuffer, value.VertexBuffer)
                || !ReferenceEquals(current.IndexBuffer, value.IndexBuffer))
            {
                current = new ChunkDrawBatch(region, value.VertexBuffer, value.IndexBuffer, _commands.Count);
                _batches.Add(current);
            }
            current.CommandCount++;
            _commands.Add(new IndirectDrawCommand
            {
                IndexCount = (uint)value.IndexCount,
                InstanceCount = 1,
                FirstIndex = (uint)value.FirstIndex,
                VertexOffset = value.BaseVertex,
                FirstInstance = 0
            });
        }
    }

    //EnsureIndirectCapacity returns a command buffer large enough for the frame, growing it when needed
    private GpuBuffer EnsureIndirectCapacity(long bytes)
    {
        if (_indirectCommands is not null && _indirectCapacity >= bytes) return _indirectCommands;
        if (_indirectCommands is not null)
        {
            _bufferPool.ReturnBuffer(_indirectCommands);
            _indirectCommands = null;
        }
        var capacity = Math.Max(4096L, bytes);
        _indirectCommands = _bufferPool.GetBuffer(capacity, GpuBuffer.UsageIndirectParameters | GpuBuffer.UsageCopyDst);
        _indirectCapacity = capacity;
        return _indirectCommands;
    }

    public void Dispose()
    {
        _level.SectionDirty -= _sectionDirtyHandler;
        _level.ChunkUnloaded -= _chunkUnloadedHandler;
        if (_indirectCommands is not null)
        {
            _bufferPool.ReturnBuffer(_indirectCommands);
            _indirectCommands = null;
            _indirectCapacity = 0;
        }
        //The entity dispatcher is disposed along with world rendering; it holds vertex/index buffers borrowed from the buffer pool
        EntityDispatcher?.Dispose();
        EntityDispatcher = null;
        MovingBlocks?.Dispose();
        MovingBlocks = null;
    }

    private static readonly RenderLayer[] s_layers = { RenderLayer.Solid, RenderLayer.Cutout, RenderLayer.Translucent };

    //ChunkDrawBatch one indirect draw batch: a region plus the arena buffers its sections share
    private sealed class ChunkDrawBatch
    {
        public ChunkDrawBatch(RenderRegion region, GpuBuffer vertexBuffer, GpuBuffer indexBuffer, int firstCommand)
        {
            Region = region;
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
            FirstCommand = firstCommand;
        }

        public RenderRegion Region { get; }
        public GpuBuffer VertexBuffer { get; }
        public GpuBuffer IndexBuffer { get; }
        public int FirstCommand { get; }
        public int CommandCount { get; set; }
    }

    //IndirectDrawCommand the VkDrawIndexedIndirectCommand layout, 20 bytes
    [StructLayout(LayoutKind.Sequential, Size = Size)]
    private struct IndirectDrawCommand
    {
        public const int Size = 20;
        public uint IndexCount;
        public uint InstanceCount;
        public uint FirstIndex;
        public int VertexOffset;
        public uint FirstInstance;
    }
}
