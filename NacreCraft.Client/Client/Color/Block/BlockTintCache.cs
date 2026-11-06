using System.Threading;
using NacreCraft.Primitives;

namespace NacreCraft.Client.Color.Block;

//BlockTintCache caches the tint color per block position and chunk, maps to vanilla net.minecraft.client.color.block.BlockTintCache
public sealed class BlockTintCache
{
    private const int MaxCacheEntries = 256;
    private const int BlocksPerLayer = 16 * 16;

    private readonly ThreadLocal<LatestCacheInfo> _latestChunkOnThread = new(() => new LatestCacheInfo());
    private readonly Dictionary<long, CacheData> _cache = new();
    private readonly LinkedList<long> _insertionOrder = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Func<BlockPos, int> _source;

    public BlockTintCache(Func<BlockPos, int> source) => _source = source;

    public int GetColor(BlockPos pos)
    {
        var chunkX = pos.X >> 4;
        var chunkZ = pos.Z >> 4;
        var chunkInfo = _latestChunkOnThread.Value!;
        if (chunkInfo.X != chunkX || chunkInfo.Z != chunkZ || chunkInfo.Cache is null || chunkInfo.Cache.Invalidated)
        {
            chunkInfo.X = chunkX;
            chunkInfo.Z = chunkZ;
            chunkInfo.Cache = FindOrCreateChunkCache(chunkX, chunkZ);
        }

        var layer = chunkInfo.Cache.GetLayer(pos.Y);
        var index = ((pos.Z & 15) << 4) | (pos.X & 15);
        var cached = layer[index];
        if (cached != -1) return cached;

        var calculated = _source(pos);
        layer[index] = calculated;
        return calculated;
    }

    public void InvalidateForChunk(int chunkX, int chunkZ)
    {
        _lock.EnterWriteLock();
        try
        {
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (var offsetZ = -1; offsetZ <= 1; offsetZ++)
                {
                    var key = ChunkPos.Pack(chunkX + offsetX, chunkZ + offsetZ);
                    if (!_cache.Remove(key, out var removed)) continue;
                    removed.Invalidate();
                    _insertionOrder.Remove(key);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void InvalidateAll()
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var data in _cache.Values) data.Invalidate();
            _cache.Clear();
            _insertionOrder.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private CacheData FindOrCreateChunkCache(int x, int z)
    {
        var key = ChunkPos.Pack(x, z);
        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var existing)) return existing;
        }
        finally
        {
            _lock.ExitReadLock();
        }

        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(key, out var existingNow)) return existingNow;

            if (_cache.Count >= MaxCacheEntries && _insertionOrder.First is { } first)
            {
                _insertionOrder.RemoveFirst();
                if (_cache.Remove(first.Value, out var evicted)) evicted.Invalidate();
            }

            var newCache = new CacheData();
            _cache[key] = newCache;
            _insertionOrder.AddLast(key);
            return newCache;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private sealed class LatestCacheInfo
    {
        public int X = int.MinValue;
        public int Z = int.MinValue;
        public CacheData? Cache;
    }

    private sealed class CacheData
    {
        private readonly Dictionary<int, int[]> _layers = new();
        private readonly ReaderWriterLockSlim _lock = new();
        private volatile bool _invalidated;

        public bool Invalidated => _invalidated;

        public int[] GetLayer(int y)
        {
            _lock.EnterReadLock();
            try
            {
                if (_layers.TryGetValue(y, out var existing)) return existing;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            _lock.EnterWriteLock();
            try
            {
                if (_layers.TryGetValue(y, out var existing)) return existing;
                var layer = new int[BlocksPerLayer];
                Array.Fill(layer, -1);
                _layers[y] = layer;
                return layer;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Invalidate() => _invalidated = true;
    }
}
