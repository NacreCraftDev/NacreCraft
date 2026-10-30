using System.Collections.Concurrent;
using System.Threading;
using NacreCraft.Client.Level;
using NacreCraft.Client.Render.Culling;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Primitives;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
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

//SectionRenderDispatcher async dispatcher, maps to vanilla SectionRenderDispatcher
//Background threads compile chunk meshes + mesh cache + dirty marking; the Render thread only does Upload+Draw
//_sections holds all RenderSections; _compileQueue is consumed by background threads, _uploadQueue by the Render thread
//_mesh is published atomically with Interlocked.Exchange; the compile thread writes and the Render thread reads
//_syncRoot is a coarse lock protecting only the Render thread's combined Upload+Draw; compile threads do not take the lock
//Vertex and index data live in region-level BufferArenas: a section carves byte ranges out of its region's arena
//instead of owning a buffer, so one region binds a single buffer per kind and can be batched later
public sealed class SectionRenderDispatcher : IDisposable
{
    private readonly ClientLevel _level;
    private readonly ChunkMeshBuilder _meshBuilder;
    private readonly GpuBufferPool _bufferPool;
    private readonly ConcurrentDictionary<long, RenderSection> _sections = new();
    //_regions region-level arenas, one entry per 8x4x8 section region that has live sections
    private readonly ConcurrentDictionary<long, RenderRegion> _regions = new();
    //_compileQueue background threads block on BlockingCollection.GetConsumingEnumerable
    private readonly BlockingCollection<RenderSection> _compileQueue = new(new ConcurrentQueue<RenderSection>());
    //_uploadQueue the compile thread Enqueues, the Render thread TryDequeues inside the Lock
    private readonly ConcurrentQueue<RenderSection> _uploadQueue = new();
    private readonly Thread[] _workers;
    private readonly object _syncRoot = new();
    private readonly ViewArea _viewArea;
    private const int VertexStride = ChunkVertexFormat.Stride;
    private volatile bool _running;
    private bool _disposed;

    public int SectionCount => _sections.Count;
    public int PendingUploadCount => _uploadQueue.Count;
    public int VisibleSectionCount => _viewArea.VisibleCount;
    //BufferPoolInUseCount currently borrowed GPU buffer count; with arenas this counts the arena blocks in use
    public int BufferPoolInUseCount => _bufferPool.InUseCount;
    //UploadedSectionCount number of uploaded sections; iterates _sections counting Uploaded state, for tests
    public int UploadedSectionCount
    {
        get
        {
            var count = 0;
            foreach (var section in _sections.Values)
                if (section.State == RenderSectionState.Uploaded) count++;
            return count;
        }
    }

    //workerCount defaults to max(1, ProcessorCount-1); CPU-bound work uses dedicated threads, not the ThreadPool
    public SectionRenderDispatcher(ClientLevel level, ChunkMeshBuilder meshBuilder, GpuBufferPool bufferPool, int? workerCount = null)
    {
        _level = level;
        _meshBuilder = meshBuilder;
        _bufferPool = bufferPool;
        _viewArea = new ViewArea(level);
        var count = workerCount ?? Math.Max(1, Environment.ProcessorCount - 1);
        _workers = new Thread[count];
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _viewArea.Start();
        for (var i = 0; i < _workers.Length; i++)
        {
            _workers[i] = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"SectionRenderDispatcher-Worker-{i}",
                Priority = ThreadPriority.BelowNormal,
            };
            _workers[i].Start();
        }
    }

    //Stop calls CompleteAdding so workers exit GetConsumingEnumerable, then Join waits for the threads to end
    public void Stop()
    {
        if (!_running) return;
        _running = false;
        _compileQueue.CompleteAdding();
        foreach (var worker in _workers)
            worker?.Join();
    }

    //SetCameraPosition called on the Render thread; the frustum goes to the cull worker and its newest result is
    //adopted here, marking newly visible sections dirty so they get compiled
    public void SetCameraPosition(Camera camera, Frustum frustum)
    {
        _viewArea.RequestUpdate(frustum);
        _viewArea.SyncVisible(MarkDirty);
    }

    //MarkDirty marks dirty and enqueues the section + 6 neighbors; cross-section boundary face culling must be recomputed
    //Unloaded neighbor sections are skipped without creating a RenderSection, to avoid _sections bloat
    public void MarkDirty(SectionPos pos)
    {
        MarkDirtySingle(pos);
        MarkDirtySingle(new SectionPos(pos.X + 1, pos.Y, pos.Z));
        MarkDirtySingle(new SectionPos(pos.X - 1, pos.Y, pos.Z));
        MarkDirtySingle(new SectionPos(pos.X, pos.Y + 1, pos.Z));
        MarkDirtySingle(new SectionPos(pos.X, pos.Y - 1, pos.Z));
        MarkDirtySingle(new SectionPos(pos.X, pos.Y, pos.Z + 1));
        MarkDirtySingle(new SectionPos(pos.X, pos.Y, pos.Z - 1));
    }

    private void MarkDirtySingle(SectionPos pos)
    {
        if (_level.GetSection(pos.X, pos.Y, pos.Z) is null) return;
        var section = _sections.GetOrAdd(pos.AsLong(), _ => new RenderSection(pos));
        if (section.Region is null)
        {
            var regionPos = RenderRegionPos.Of(pos);
            var region = _regions.GetOrAdd(regionPos.AsLong(), _ => new RenderRegion(regionPos, _bufferPool));
            var slot = RenderRegion.SlotIndex(pos.X, pos.Y, pos.Z);
            section.Region = region;
            section.Slot = slot;
            region.SetSection(slot, section);
        }
        if (section.TryMarkDirty())
        {
            try { _compileQueue.Add(section); }
            catch (InvalidOperationException) { } //Add throws after CompleteAdding; ignore
        }
    }

    //Lock/Unlock the Render thread holds the lock during Upload+Draw; compile threads do not take the lock and only publish mesh atomically
    public void Lock() => Monitor.Enter(_syncRoot);
    public void Unlock() => Monitor.Exit(_syncRoot);

    //UploadTerrainBuffers called inside the Render thread's Lock; dequeues everything from _uploadQueue and uploads
    //Each section concatenates its layers into one vertex range and one index range carved out of its region arenas
    //Layer vertices are concatenated and indices offset by baseVertex; UploadedSlice is recorded for GetSectionSlice
    public void UploadTerrainBuffers()
    {
        while (_uploadQueue.TryDequeue(out var section))
        {
            //Skip unloaded sections; UnloadSection already detached them from their region
            if (section.IsRemoved) continue;
            var region = section.Region;
            if (region is null) continue;
            var mesh = section.Mesh;
            if (mesh is null || mesh.TotalVertexCount == 0)
            {
                //Empty mesh: nothing to upload, but it still counts as uploaded so Draw skips it
                section.ClearUploaded();
                section.SetUploaded(default, default);
                continue;
            }

            var vertexData = new byte[mesh.TotalVertexCount * VertexStride];
            var indexData = new int[mesh.TotalIndexCount];
            var vertexByteOffset = 0;
            var indexOffset = 0;
            var baseVertex = 0;
            var firstIndex = 0;
            foreach (var layer in s_layers)
            {
                if (!mesh.HasLayer(layer)) continue;
                var vertices = mesh.GetVertices(layer);
                var vertexCount = mesh.GetVertexCount(layer);
                var indices = mesh.GetIndices(layer);
                vertices.CopyTo(new Span<byte>(vertexData, vertexByteOffset, vertices.Length));
                for (var i = 0; i < indices.Length; i++)
                    indexData[indexOffset + i] = indices[i] + baseVertex;
                section.Slices[(int)layer] = new UploadedSlice(baseVertex, firstIndex, indices.Length);
                vertexByteOffset += vertices.Length;
                indexOffset += indices.Length;
                baseVertex += vertexCount;
                firstIndex += indices.Length;
            }

            //The buffers are indexed by absolute vertex/index number, so the per-layer offsets shift by the range base
            var vertexRange = region.UploadVertexData(section.Pos.AsLong(), vertexData);
            var indexRange = region.UploadIndexData(section.Pos.AsLong(),
                System.Runtime.InteropServices.MemoryMarshal.AsBytes<int>(indexData));
            var bufferBaseVertex = (int)(vertexRange.Offset / VertexStride);
            var bufferBaseIndex = (int)(indexRange.Offset / sizeof(int));
            for (var layer = 0; layer < s_layers.Length; layer++)
            {
                if (section.Slices[layer] is not { } slice) continue;
                section.Slices[layer] = new UploadedSlice(
                    bufferBaseVertex + slice.BaseVertex,
                    bufferBaseIndex + slice.FirstIndex,
                    slice.IndexCount);
            }
            section.SetUploaded(vertexRange, indexRange);
        }
    }

    //GetSectionSlice called inside the Render thread's Lock; returns the layer's buffers + offsets
    //Returns null when State!=Uploaded or the layer has no vertices; Draw skips
    public SectionSlice? GetSectionSlice(SectionPos pos, RenderLayer layer)
    {
        if (!_sections.TryGetValue(pos.AsLong(), out var section)) return null;
        if (section.State != RenderSectionState.Uploaded) return null;
        if (!section.HasUploadedRanges || section.VertexRange.Buffer is null) return null;
        if (section.Slices[(int)layer] is not { } slice) return null;
        return new SectionSlice(section.VertexRange.Buffer, section.IndexRange.Buffer, slice.BaseVertex, slice.FirstIndex, slice.IndexCount);
    }

    //EnumerateVisibleSections called inside the Render thread's Lock; iterates uploaded sections and tests the frustum
    //Also filters by _viewArea.IsVisible to avoid rendering sections of unloaded chunks
    //ConcurrentDictionary.Values returns a snapshot so traversal is safe; the yield is consumed immediately by the foreach inside the Lock
    public IEnumerable<RenderSection> EnumerateVisibleSections(Frustum frustum)
    {
        foreach (var section in _sections.Values)
        {
            if (section.State != RenderSectionState.Uploaded) continue;
            if (!_viewArea.IsVisible(section.Pos)) continue;
            if (frustum.IsVisible(section.BoundingBox)) yield return section;
        }
    }

    //UnloadSection called on chunk unload inside the Render thread's Lock; detaches the section and drops its region once empty
    //MarkRemoved prevents races: the compile thread checks before PublishMesh and skips; the upload thread checks after dequeue and skips
    public void UnloadSection(SectionPos pos)
    {
        if (!_sections.TryRemove(pos.AsLong(), out var section)) return;
        section.MarkRemoved();
        var region = section.Region;
        if (region is null) return;
        region.SetSection(section.Slot, null);
        //Drops the section's ranges; once a heap block empties out it is closed on the next upload
        region.Release(pos.AsLong());
        if (region.LiveSectionCount > 0) return;
        _regions.TryRemove(region.Pos.AsLong(), out _);
        region.Dispose();
    }

    //WorkerLoop background thread main loop; blocks on GetConsumingEnumerable, and after compiling enqueues into _uploadQueue
    //Holds ClientLevel's read lock while calling GetSection+Snapshot+Build, preventing SetBlockState from modifying the section's internal PalettedContainer
    //PublishMesh+Enqueue happen outside the read lock to reduce lock hold time; if the chunk is unloaded and rawSection is null, skip and leave it in Compiling
    private void WorkerLoop()
    {
        foreach (var section in _compileQueue.GetConsumingEnumerable())
        {
            if (!_running) break;
            if (!section.TryBeginCompile()) continue;
            SectionMesh? mesh = null;
            var compileSuccess = false;
            try
            {
                _level.EnterReadLock();
                try
                {
                    var rawSection = _level.GetSection(section.Pos.X, section.Pos.Y, section.Pos.Z);
                    if (rawSection is null)
                    {
                        //Chunk already unloaded; the section is stuck in Compiling until UnloadSection cleans it up, not handled in the first version
                        compileSuccess = false;
                    }
                    else if (rawSection.HasOnlyAir())
                    {
                        mesh = new SectionMesh();
                        compileSuccess = true;
                    }
                    else
                    {
                        var regionCache = RenderRegionCache.Snapshot(_level, section.Pos);
                        var originX = section.Pos.X << 4;
                        var originY = section.Pos.Y << 4;
                        var originZ = section.Pos.Z << 4;
                        var buffers = _meshBuilder.Build(rawSection, regionCache, originX, originY, originZ, section.Slot);
                        mesh = SectionMesh.FromChunkBuildBuffers(buffers);
                        compileSuccess = true;
                    }
                }
                finally { _level.ExitReadLock(); }
                //The section was unloaded during compilation; skip publishing, UnloadSection already detached it
                if (compileSuccess && mesh is not null && !section.IsRemoved)
                {
                    section.PublishMesh(mesh);
                    _uploadQueue.Enqueue(section);
                }
            }
            catch
            {
                if (_running && section.TryMarkDirty())
                {
                    try { _compileQueue.Add(section); } catch (InvalidOperationException) { }
                }
            }
        }
    }

    private static readonly RenderLayer[] s_layers = { RenderLayer.Solid, RenderLayer.Cutout, RenderLayer.Translucent };

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _viewArea.Dispose();
        _compileQueue.Dispose();
        foreach (var region in _regions.Values)
            region.Dispose();
        _regions.Clear();
        _sections.Clear();
        _disposed = true;
    }
}

//SectionSlice the render slice returned by dispatcher.GetSectionSlice for LevelRenderer.Draw to submit
//The buffers are the section's region arenas; BaseVertex is the vertex offset inside the vertex arena and
//FirstIndex the index offset inside the index arena, so DrawIndexed can address the section without rebinding
public readonly record struct SectionSlice(GpuBuffer VertexBuffer, GpuBuffer IndexBuffer, int BaseVertex, int FirstIndex, int IndexCount);
