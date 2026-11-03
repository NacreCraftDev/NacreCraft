using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Logging;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Storage;
using NacreCraft.Storage.Light;

namespace NacreCraft.Game.Server;

//ChunkSender progressive chunk sender, maps to vanilla PlayerChunkSender
//UpdateCenter updates the view center when a player crosses a chunk; chunks entering view are queued and those leaving are forgotten, maps to vanilla updateChunkTracking+applyChunkTrackingView
//Tick sends a batch of BatchStart + ready chunks + BatchFinished per tick; not-ready ones stay in the pending set for the next tick
//Generation advances in the ServerChunkCache background without blocking the main loop; it terminates permanently after disconnect
public sealed class ChunkSender
{
    //MaxChunksPerTick ceiling per tick, aligned with Paper's getMaxChunkSendRate default of ten thousand chunks per second
    //The old four per tick cap made a whole view take twenty seconds to arrive
    private const int MaxChunksPerTick = 10000;
    private const int MinChunksPerTick = 1;

    //_desiredChunksPerTick the batch size the client asked for through chunk_batch_received, maps to vanilla desiredChunksPerTick
    //The client measures how long a batch took to apply and answers with what it can keep up with, so a slow client slows the
    //sender down instead of building a backlog it will never drain
    private int _desiredChunksPerTick = MaxChunksPerTick;

    private readonly Connection _connection;
    private readonly Func<ChunkPos, ChunkAccess?> _provider;
    private readonly Func<ChunkPos, bool>? _isFailed;
    //_lightSource light data source; empty sends empty light
    private readonly ServerChunkCache? _lightSource;
    //_blockEntityBridge block entity data source; empty means the chunk packet carries no block entities
    private readonly IBlockEntityBridge? _blockEntityBridge;
    //Pending coordinates in queue order; each tick takes a batch of ready ones from the front
    //Vanilla pendingChunks is also an insertion-order queue; this project scanning everything and sorting every tick was pure overhead
    private readonly List<long> _pending = new();
    private readonly HashSet<long> _pendingSet = new();
    //_sent coordinates already delivered; a chunk stays in _tracked after sending, so this is what keeps it out of _pending
    private readonly HashSet<long> _sent = new();
    //Coordinates already queued within view, including sent ones; used when leaving view to decide whether to send Forget
    private readonly HashSet<long> _tracked = new();
    private int _centerX = int.MinValue;
    private int _centerZ;
    private int _viewDistance = 1;
    //_checkRadiusSquared squared radius in which neighbours are worth testing; the load ring just outside the view is
    //ticketed on purpose, so neighbours there exist and are safe to read without starting a load
    private int _checkRadiusSquared = 4;
    private int _sentCount;
    private int _failedCount;
    //_blockedSince when the send head first became unready, zero while sending progresses; drives the diagnostic line
    private long _blockedSince;
    private long _lastBlockLog;
    //Last view radius and simulation distance pushed to the client, maps to lastSentChunkRadius and
    //lastSentSimulationDistance in Paper's PlayerChunkLoaderData; the client sizes its own view and request rate from them
    private int _sentRadius = -1;
    private int _sentSimulationDistance = -1;
    private bool _closed;

    public ChunkSender(Connection connection, Func<ChunkPos, ChunkAccess?> provider,
        Func<ChunkPos, bool>? isFailed = null, ServerChunkCache? lightSource = null,
        IBlockEntityBridge? blockEntityBridge = null)
    {
        _connection = connection;
        _provider = provider;
        _isFailed = isFailed;
        _lightSource = lightSource;
        _blockEntityBridge = blockEntityBridge;
    }

    //PendingCount number of chunks waiting to be sent, for diagnostics
    public int PendingCount => _pending.Count;

    //IsPending whether the chunk is queued but not handed to the client yet, maps to vanilla PlayerChunkSender.isPending
    public bool IsPending(long packedPos) => _pendingSet.Contains(packedPos);

    //IsTracked whether the chunk is inside the player's view, maps to the vanilla chunk tracking view
    public bool IsTracked(long packedPos) => _tracked.Contains(packedPos);

    //TrackedChunks packed positions of every chunk inside the view
    public IEnumerable<long> TrackedChunks => _tracked;

    //FailedCount number of load-failed chunks dropped, for diagnostics
    public int FailedCount => _failedCount;

    //Temporary instrumentation: distinguishes "the sender is never handed a new view center" from "it is, but nothing enters view"
    public static long DiagnosticUpdateCenterCalls;
    public static long DiagnosticCenterMoves;
    public static long DiagnosticChunksQueued;
    //Which early exit the sender takes: a queue with entries but no work at all means one of the first two
    public static long DiagnosticTickClosed;
    public static long DiagnosticTickDisconnected;
    public static long DiagnosticTickEmpty;

    //UpdateCenter updates the view center; chunks entering view are queued and leaving ones forgotten, and SetChunkCacheCenter is synced
    //Returns early when neither the center nor the view distance changed, to avoid resending packets
    public void UpdateCenter(int centerChunkX, int centerChunkZ, int viewDistance)
    {
        var radius = Math.Clamp(viewDistance, 1, 32);
        DiagnosticUpdateCenterCalls++;
        if (_centerX != int.MinValue && _centerX == centerChunkX && _centerZ == centerChunkZ && _viewDistance == radius)
            return;
        DiagnosticCenterMoves++;
        _centerX = centerChunkX;
        _centerZ = centerChunkZ;
        _viewDistance = radius;
        var radiusSquared = radius * radius;
        //Neighbours one ring outside the view are ticketed by the loader, so the gate may consult them too
        _checkRadiusSquared = (radius + 1) * (radius + 1);
        //Coordinates entering view are tracked; the circular test matches the initial queueing rule
        for (var dx = -radius; dx <= radius; dx++)
        for (var dz = -radius; dz <= radius; dz++)
        {
            if (dx * dx + dz * dz > radiusSquared) continue;
            _tracked.Add(ChunkPos.Pack(centerChunkX + dx, centerChunkZ + dz));
        }
        //Queue only what is already loaded and lit. Paper's sendQueue holds finished chunks and is filled when generation
        //completes; queueing every coordinate entering the view made the head a chunk that was still generating, and the
        //sender then stopped on it every tick, which is what turned a view into a twenty second trickle
        FillPending();
        //Forgetting on leaving view: unsent ones are only dequeued, sent ones send a Forget packet, maps to vanilla dropChunk
        foreach (var key in _tracked.Where(key => DistanceSquared(ChunkPos.Unpack(key)) > radiusSquared).ToList())
            DropChunk(key);
        //The client sizes its own view and its chunk request rate from these two, maps to the radius and simulation
        //distance sync at the end of Paper's update(); a stale value leaves it waiting for a range the server never sends
        if (_sentRadius != radius)
        {
            _sentRadius = radius;
            _connection.Send(new ClientboundSetChunkCacheRadiusPacket(radius));
        }
        if (_lightSource is { } source && _sentSimulationDistance != source.SimulationDistance)
        {
            _sentSimulationDistance = source.SimulationDistance;
            _connection.Send(new ClientboundSetSimulationDistancePacket(source.SimulationDistance));
        }
        _connection.Send(new ClientboundSetChunkCacheCenterPacket(centerChunkX, centerChunkZ));
    }

    //FillPending moves every tracked chunk that is loaded and lit into the send queue, maps to the sendQueue of Paper's
    //RegionizedPlayerChunkLoader, which is fed by the generation pipeline instead of being polled
    //A chunk whose light is not built yet stays in _tracked and is picked up on a later tick
    private void FillPending()
    {
        var added = false;
        foreach (var key in _tracked)
        {
            if (_sent.Contains(key) || _pendingSet.Contains(key)) continue;
            var pos = ChunkPos.Unpack(key);
            if (_isFailed?.Invoke(pos) == true) continue;
            //Ask the provider first: it also submits the load for a chunk that has none, which is what keeps the view
            //generating when the sender is the only thing touching it
            if (_provider(pos) is null) continue;
            if (_lightSource is not null && !_lightSource.IsLightReady(pos)) continue;
            _pending.Add(key);
            _pendingSet.Add(key);
            DiagnosticChunksQueued++;
            added = true;
        }
        if (added) _pending.Sort((a, b) => DistanceSquared(ChunkPos.Unpack(a)).CompareTo(DistanceSquared(ChunkPos.Unpack(b))));
    }

    //DropChunk forgets a single chunk; only one already sent needs to notify the client
    private void DropChunk(long key)
    {
        _tracked.Remove(key);
        //Dropping the sent mark is what lets the chunk be queued again if the view comes back to it
        _sent.Remove(key);
        if (_pendingSet.Remove(key))
        {
            _pending.Remove(key);
            return;
        }
        if (_connection.IsConnected)
            _connection.Send(new ClientboundForgetLevelChunkPacket(ChunkPos.Unpack(key)));
    }

    private int DistanceSquared(ChunkPos pos)
    {
        var dx = pos.X - _centerX;
        var dz = pos.Z - _centerZ;
        return dx * dx + dz * dz;
    }

    //Tick sends the nearest ready chunks in queue order, stopping at the first one that is not ready
    //The queue is distance ordered, so a chunk the client cannot use yet blocks everything behind it instead of being
    //skipped: a hole next to the player is worse than a slightly later batch
    public void Tick()
    {
        if (_closed) { DiagnosticTickClosed++; return; }
        //If the connection is closed, sending terminates and state is cleared, avoiding an ObjectDisposedException every tick flooding the log
        if (!_connection.IsConnected)
        {
            DiagnosticTickDisconnected++;
            Log.Debug($"Chunk sending aborted, connection closed remaining={_pending.Count}");
            ClearQueue();
            _closed = true;
            return;
        }
        //Chunks that finished generating since the last tick are picked up here, which is what keeps the queue fed
        //between view moves; Paper does the same by pushing into sendQueue as generation completes
        FillPending();
        if (_pending.Count == 0)
        {
            DiagnosticTickEmpty++;
            return;
        }
        var perTick = Math.Clamp(_desiredChunksPerTick, MinChunksPerTick, MaxChunksPerTick);
        var batch = new List<(ChunkPos Pos, ChunkAccess Chunk)>(perTick);
        var index = 0;
        var scanStart = TickStageProfiler.Now();
        while (index < _pending.Count && batch.Count < perTick)
        {
            var key = _pending[index];
            var pos = ChunkPos.Unpack(key);
            if (_isFailed?.Invoke(pos) == true)
            {
                _pending.RemoveAt(index);
                _pendingSet.Remove(key);
                _tracked.Remove(key);
                _failedCount++;
                continue;
            }
            var chunk = _provider(pos);
            //Sending runs strictly from the nearest chunk outward: skipping a head that is not ready would hand the client a
            //hole it can never fill, because that chunk is not retried once the batch moves past it. Vanilla breaks here too
            if (chunk is null)
            {
                //The head blocks every chunk behind it, so ask for it at the priority of a chunk the player is standing on;
                //without this it sits at whatever ticket level it had when the view moved, often behind the whole view
                _lightSource?.RaisePriority(pos.X, pos.Z, ChunkHolder.TickingLevel);
                Blocked(pos, "not-loaded");
                break;
            }
            //A chunk whose light has not been built yet stays queued for a later tick
            //Sending it now would leave the client with a black chunk until an incremental light update arrived for every section
            if (_lightSource is not null && !_lightSource.IsLightReady(pos)) { Blocked(pos, "no-light"); break; }
            if (!AreNeighboursGenerated(pos, out var neighbourDetail, out var missing))
            {
                //A missing neighbour is what the head actually waits on; raising it releases the batch
                _lightSource?.RaisePriority(missing.X, missing.Z, ChunkHolder.TickingLevel);
                Blocked(pos, $"neighbour {neighbourDetail}");
                break;
            }
            batch.Add((pos, chunk));
            _pending.RemoveAt(index);
            _pendingSet.Remove(key);
            //Stays in _tracked so leaving view later still sends a Forget; _sent keeps FillPending from re-queueing it
            _sent.Add(key);
        }
        if (batch.Count > 0) _blockedSince = 0;
        TickStageProfiler.Record(TickStage.ChunkScan, scanStart);
        if (batch.Count == 0) return;
        try
        {
            _connection.Send(new ClientboundChunkBatchStartPacket());
            foreach (var (pos, chunk) in batch)
            {
                var prepStart = TickStageProfiler.Now();
                //Chunk serialization is heavy and does not touch the light engine, so it runs outside the light lock
                //The light data itself is read straight off the published snapshot, so building the packet needs no lock at all
                var lightSource = _lightSource;
                //Block entities are sent with the chunk so the client sees existing block entities on join
                var blockEntities = _blockEntityBridge?.Collect(pos);
                var packet = lightSource is null
                    ? ClientboundLevelChunkWithLightPacket.CreatePrepared(
                        pos.X, pos.Z, chunk, () => ClientboundLightUpdatePacketData.Empty, blockEntities)
                    : ClientboundLevelChunkWithLightPacket.CreatePrepared(
                        pos.X, pos.Z, chunk,
                        () => new ClientboundLightUpdatePacketData(pos, lightSource.LightEngine),
                        blockEntities);
                TickStageProfiler.Record(TickStage.ChunkPrep, prepStart);

                var sendStart = TickStageProfiler.Now();
                _connection.Send(packet);
                TickStageProfiler.Record(TickStage.ChunkSend, sendStart);
                _sentCount++;
            }
            _connection.Send(new ClientboundChunkBatchFinishedPacket(batch.Count));
        }
        catch (Exception e)
        {
            _closed = true;
            //The connection being closed by the peer during sending is a normal race; terminate silently without a false report
            if (!_connection.IsConnected)
            {
                Log.Debug($"Chunk sending aborted, connection closed remaining={_pending.Count}");
                ClearQueue();
                return;
            }
            Log.Warning($"Chunk send failed remaining={_pending.Count} {e.Message} connAlive={_connection.IsConnected} details={_connection.DisconnectionDetails?.Reason ?? "none"}");
        }
    }

    //OnChunkBatchReceived takes the batch size the client reports it can keep up with, maps to vanilla PlayerChunkSender.onChunkBatchReceived
    //The client answers after applying a batch, so this is the only back pressure the sender has; without it a slow client
    //accumulates a backlog of chunk packets it applies long after they were built
    public void OnChunkBatchReceived(float desiredChunksPerTick)
    {
        if (float.IsNaN(desiredChunksPerTick)) return;
        _desiredChunksPerTick = (int)Math.Clamp(desiredChunksPerTick, MinChunksPerTick, MaxChunksPerTick);
    }

    //Blocked records why the send head is stuck and reports every two seconds; the head blocks the whole queue, so a
    //single unready chunk near the player is otherwise indistinguishable from a dead connection
    private void Blocked(ChunkPos pos, string reason)
    {
        var now = Environment.TickCount64;
        if (_blockedSince == 0) _blockedSince = now;
        if (now - _lastBlockLog < 2000) return;
        _lastBlockLog = now;
        Log.Info($"[chunk-send] headBlocked=[{pos.X},{pos.Z}] reason={reason} for={now - _blockedSince}ms pending={_pending.Count}");
    }

    //AreNeighboursGenerated checks the eight surrounding chunks, maps to vanilla PlayerChunkSender.areNeighboursGenerated
    //A chunk sent before its neighbours are generated leaves the client with sections it cannot mesh or light correctly,
    //and it is never resent. Only neighbours already inside the view are considered: asking the provider for a chunk outside
    //it starts a load with no ticket behind it, which never finishes and stalls the whole queue
    private bool AreNeighboursGenerated(ChunkPos pos, out string detail, out ChunkPos missing)
    {
        detail = string.Empty;
        missing = default;
        for (var offsetX = -1; offsetX <= 1; offsetX++)
        for (var offsetZ = -1; offsetZ <= 1; offsetZ++)
        {
            if (offsetX == 0 && offsetZ == 0) continue;
            var neighbourX = pos.X + offsetX;
            var neighbourZ = pos.Z + offsetZ;
            //Beyond the ticketed band: nothing to wait for, and asking the provider there would start a load
            var diffX = neighbourX - _centerX;
            var diffZ = neighbourZ - _centerZ;
            if (diffX * diffX + diffZ * diffZ > _checkRadiusSquared) continue;
            var neighbour = _provider(new ChunkPos(neighbourX, neighbourZ));
            if (neighbour is null)
            {
                //A neighbour that already failed will never arrive; waiting on it would freeze the queue for good
                if (_isFailed?.Invoke(new ChunkPos(neighbourX, neighbourZ)) == true) continue;
                detail = $"[{neighbourX},{neighbourZ}]:absent";
                missing = new ChunkPos(neighbourX, neighbourZ);
                return false;
            }
            var status = neighbour.ChunkStatus;
            if (status is null || !status.IsOrAfter(ChunkStatus.FULL))
            {
                detail = $"[{neighbourX},{neighbourZ}]:{status?.Name ?? "null"}";
                missing = new ChunkPos(neighbourX, neighbourZ);
                return false;
            }
        }
        return true;
    }

    private void ClearQueue()
    {
        _pending.Clear();
        _pendingSet.Clear();
        _tracked.Clear();
        _sent.Clear();
    }
}
