using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage.Chunk;

namespace NacreCraft.Storage;

//GeneratingChunkMap the set of chunks currently generating, maps to vanilla net.minecraft.server.level.GeneratingChunkMap
//Tracks one ChunkGenerationTask per chunk so a second request joins the running task instead of starting another
public sealed class GeneratingChunkMap(ChunkMap chunkMap)
{
    private readonly Dictionary<ChunkPos, ChunkGenerationTask> _tasks = new();

    public ChunkMap ChunkMap { get; } = chunkMap;

    public IReadOnlyCollection<ChunkGenerationTask> Tasks => _tasks.Values;

    //GetOrCreateTask reuses a running task or records a new one, maps to vanilla schedule
    public ChunkGenerationTask GetOrCreateTask(ChunkPos pos, ChunkHolder holder, ChunkStatus targetStatus)
    {
        if (_tasks.TryGetValue(pos, out var existing) && !existing.IsComplete) return existing;
        var task = new ChunkGenerationTask(holder, targetStatus);
        _tasks[pos] = task;
        return task;
    }

    //Complete drops a finished task
    public void Complete(ChunkPos pos) => _tasks.Remove(pos);

    public bool IsGenerating(ChunkPos pos) => _tasks.ContainsKey(pos);
}
