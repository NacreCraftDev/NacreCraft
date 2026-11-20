using NacreCraft.Primitives;
using Action = System.Action;

namespace NacreCraft.Game.Server.Network;

//PlayerChunkSender batches chunk sends for one player, maps to vanilla net.minecraft.server.network.PlayerChunkSender
//NC's connection layer is packet-agnostic here, so the actual chunk packet emission is supplied through delegates;
//the quota, batch acknowledgement and ordering logic matches vanilla
public sealed class PlayerChunkSender
{
    public const float MinChunksPerTick = 0.01f;
    public const float MaxChunksPerTick = 64.0f;
    private const float StartChunksPerTick = 9.0f;
    private const int DefaultMaxUnacknowledgedBatches = 10;

    private readonly bool _memoryConnection;
    private readonly HashSet<long> _pendingChunks = new();
    private float _batchQuota;
    private int _unacknowledgedBatches;
    private float _desiredChunksPerTick = StartChunksPerTick;
    private int _maxUnacknowledgedBatches = 1;

    public PlayerChunkSender(bool memoryConnection) => _memoryConnection = memoryConnection;

    //SendChunk emits one chunk to the player
    public Action<ChunkPos>? SendChunk { get; set; }

    //ForgetChunk tells the client to drop a chunk
    public Action<ChunkPos>? ForgetChunk { get; set; }

    //BatchStart/BatchFinished bracket a chunk batch
    public Action? BatchStart { get; set; }

    public Action<int>? BatchFinished { get; set; }

    public int PendingCount => _pendingChunks.Count;

    public int UnacknowledgedBatches => _unacknowledgedBatches;

    public void MarkChunkPendingToSend(ChunkPos pos) => _pendingChunks.Add(pos.Pack());

    public void DropChunk(ChunkPos pos)
    {
        if (!_pendingChunks.Remove(pos.Pack())) ForgetChunk?.Invoke(pos);
    }

    //CollectChunksToSend takes the nearest chunks up to the current quota and removes them from the pending set
    public IReadOnlyList<ChunkPos> CollectChunksToSend(ChunkPos playerPos)
    {
        var maxBatchSize = (int)_batchQuota;
        var candidates = _pendingChunks.Select(ChunkPos.Unpack)
            .OrderBy(pos => (pos.X - playerPos.X) * (pos.X - playerPos.X) + (pos.Z - playerPos.Z) * (pos.Z - playerPos.Z))
            .ToList();
        if (!_memoryConnection && candidates.Count > maxBatchSize) candidates = candidates.Take(Math.Max(0, maxBatchSize)).ToList();
        foreach (var pos in candidates) _pendingChunks.Remove(pos.Pack());
        return candidates;
    }

    //SendNextChunks sends one batch when quota and acknowledgement allow it, maps to vanilla sendNextChunks
    public void SendNextChunks(ChunkPos playerPos)
    {
        if (_unacknowledgedBatches >= _maxUnacknowledgedBatches) return;
        var maxBatchSize = Math.Max(1f, _desiredChunksPerTick);
        _batchQuota = Math.Min(_batchQuota + _desiredChunksPerTick, maxBatchSize);
        if (_batchQuota < 1f || _pendingChunks.Count == 0) return;
        var chunks = CollectChunksToSend(playerPos);
        if (chunks.Count == 0) return;
        _unacknowledgedBatches++;
        BatchStart?.Invoke();
        foreach (var chunk in chunks) SendChunk?.Invoke(chunk);
        BatchFinished?.Invoke(chunks.Count);
        _batchQuota -= chunks.Count;
    }

    //OnChunkBatchReceivedByClient updates the desired rate, maps to vanilla onChunkBatchReceivedByClient
    public void OnChunkBatchReceivedByClient(float desiredChunksPerTick)
    {
        _unacknowledgedBatches = Math.Max(0, _unacknowledgedBatches - 1);
        _desiredChunksPerTick = float.IsNaN(desiredChunksPerTick)
            ? MinChunksPerTick
            : Math.Clamp(desiredChunksPerTick, MinChunksPerTick, MaxChunksPerTick);
        if (_unacknowledgedBatches == 0) _batchQuota = 1f;
        _maxUnacknowledgedBatches = DefaultMaxUnacknowledgedBatches;
    }

    public bool IsPending(long packedPos) => _pendingChunks.Contains(packedPos);
}
