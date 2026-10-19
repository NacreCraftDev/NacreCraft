using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Storage;

//ChunkLoadCounter counts the chunks newly pulled in by a scheduler, maps to vanilla net.minecraft.server.level.ChunkLoadCounter
//The scheduler activates saved tickets or adds spawn tickets; only chunks not already at FULL before the call are counted
public sealed class ChunkLoadCounter
{
    private readonly List<ChunkHolder> _pendingChunks = new();
    private int _totalChunks;

    //TotalChunks how many chunks the tracked scheduler pulled in
    public int TotalChunks => _totalChunks;

    //ReadyChunks how many of the tracked chunks reached FULL
    public int ReadyChunks => _totalChunks - PendingChunks();

    //Track runs the scheduler and records the chunks it pulled in, maps to vanilla track
    public void Track(PersistentServerLevel level, System.Action scheduler)
    {
        var chunkSource = level.ChunkSource;
        var alreadyLoaded = new HashSet<long>();
        chunkSource.RunTicketTrackers();
        foreach (var holder in chunkSource.ChunkMap.AllChunksWithAtLeastStatus(ChunkStatus.FULL))
            alreadyLoaded.Add(holder.Pos.Pack());
        scheduler();
        chunkSource.RunTicketTrackers();
        foreach (var holder in chunkSource.ChunkMap.AllChunksWithAtLeastStatus(ChunkStatus.FULL))
        {
            if (alreadyLoaded.Contains(holder.Pos.Pack())) continue;
            _pendingChunks.Add(holder);
            _totalChunks++;
        }
    }

    //PendingChunks chunks still short of FULL, maps to vanilla pendingChunks
    //A holder whose future failed is dropped as well: it will never reach FULL and waiting on it would hang startup
    public int PendingChunks()
    {
        _pendingChunks.RemoveAll(h => h.Status == ChunkStatus.FULL || (h.IsDone && !h.Future.Result.IsSuccess));
        return _pendingChunks.Count;
    }
}
