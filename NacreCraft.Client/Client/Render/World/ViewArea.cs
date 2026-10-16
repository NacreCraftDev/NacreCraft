using System.Threading;
using NacreCraft.Client.Level;
using NacreCraft.Client.Render.Culling;
using NacreCraft.Primitives;
using NacreCraft.Storage.Chunk;

namespace NacreCraft.Client.Render.World;

//ViewArea keeps the set of sections inside the frustum, maps to vanilla ViewArea
//A worker thread walks the loaded chunks and tests the frustum, so the render thread only diffs the result
//Two sets are in play: the worker publishes a fresh one and never touches it again, the render thread adopts that
//result on the next sync and reports sections that just entered the frustum
//That split keeps section state (dirty marking, region binding) on the render thread, where it is not shared
public sealed class ViewArea : IDisposable
{
    private readonly ClientLevel _level;
    private readonly AutoResetEvent _signal = new(false);
    private readonly object _requestLock = new();
    //_regionVisibility worker-only cache of per-region frustum results, so the coarser test runs once per region
    private readonly Dictionary<long, bool> _regionVisibility = new();
    private Thread? _worker;
    private Frustum? _requestedFrustum;
    private volatile bool _running;
    //_computed the newest set published by the worker
    private volatile HashSet<long> _computed = new();
    //_visibleSections the set the render thread currently uses
    private HashSet<long> _visibleSections = new();
    private bool _disposed;

    public ViewArea(ClientLevel level) => _level = level;

    public int VisibleCount => _visibleSections.Count;

    public void Start()
    {
        if (_running) return;
        _running = true;
        _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "ViewArea-Cull" };
        _worker.Start();
    }

    //RequestUpdate hands the newest frustum to the worker; a pending request is simply replaced
    public void RequestUpdate(Frustum frustum)
    {
        lock (_requestLock)
            _requestedFrustum = frustum;
        _signal.Set();
    }

    //SyncVisible runs on the render thread, adopts the worker's newest result and reports newly visible sections
    //Sections that left the frustum are not reported; their meshes stay cached and are reused when visible again
    public void SyncVisible(Action<SectionPos> onNewSection)
    {
        var computed = _computed;
        if (ReferenceEquals(computed, _visibleSections)) return;
        foreach (var key in computed)
            if (!_visibleSections.Contains(key))
                onNewSection(SectionPos.Of(key));
        _visibleSections = computed;
    }

    public bool IsVisible(SectionPos pos) => _visibleSections.Contains(pos.AsLong());

    //WorkerLoop tests the frustum off the render thread while holding the level read lock
    private void WorkerLoop()
    {
        while (_running)
        {
            if (!_signal.WaitOne(100)) continue;
            if (!_running) break;
            Frustum? frustum;
            lock (_requestLock)
            {
                frustum = _requestedFrustum;
                _requestedFrustum = null;
            }
            if (frustum is null) continue;
            try
            {
                _level.EnterReadLock();
                try { _computed = ComputeVisible(frustum); }
                finally { _level.ExitReadLock(); }
            }
            catch
            {
                //A failed pass keeps the previous set; the next request retries
            }
        }
    }

    //ComputeVisible walks the loaded chunks, culls whole regions first and then the individual sections inside them
    private HashSet<long> ComputeVisible(Frustum frustum)
    {
        var visible = new HashSet<long>();
        _regionVisibility.Clear();
        foreach (var chunk in _level.GetLoadedChunks())
        {
            var sectionsCount = chunk.SectionsCount;
            for (var sy = 0; sy < sectionsCount; sy++)
            {
                var section = chunk.GetSection(sy);
                if (section is null || section.HasOnlyAir()) continue;

                var regionPos = RenderRegionPos.Of(new SectionPos(chunk.Pos.X, sy, chunk.Pos.Z));
                var regionKey = regionPos.AsLong();
                if (!_regionVisibility.TryGetValue(regionKey, out var regionVisible))
                {
                    regionVisible = frustum.IsVisible(RegionBounds(regionPos));
                    _regionVisibility[regionKey] = regionVisible;
                }
                if (!regionVisible) continue;

                var originX = chunk.Pos.X * 16;
                var originY = sy * 16;
                var originZ = chunk.Pos.Z * 16;
                if (!frustum.IsVisible(new AABB(originX, originY, originZ, originX + 16, originY + 16, originZ + 16)))
                    continue;
                visible.Add(SectionPos.AsLong(chunk.Pos.X, sy, chunk.Pos.Z));
            }
        }
        return visible;
    }

    //RegionBounds the block-space box of a region, 8x4x8 sections
    private static AABB RegionBounds(RenderRegionPos pos)
    {
        var minX = pos.X << 7;
        var minY = pos.Y << 6;
        var minZ = pos.Z << 7;
        return new AABB(minX, minY, minZ, minX + 128, minY + 64, minZ + 128);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _running = false;
        _signal.Set();
        _worker?.Join();
        _signal.Dispose();
        _disposed = true;
    }
}
