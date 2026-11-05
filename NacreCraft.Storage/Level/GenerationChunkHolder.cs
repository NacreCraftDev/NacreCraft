using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage.Chunk;

namespace NacreCraft.Storage;

//GenerationChunkHolder chunk holder seen by generation stages, maps to vanilla net.minecraft.server.level.GenerationChunkHolder
//Adds the status the stage is working towards and the generated flag on top of ChunkHolder
public sealed class GenerationChunkHolder(ChunkHolder holder)
{
    public ChunkHolder Holder { get; } = holder;

    public ChunkPos Pos => Holder.Pos;

    public ChunkAccess? Chunk => Holder.Chunk;

    public ChunkStatus CurrentStatus => Holder.Chunk?.ChunkStatus ?? ChunkStatus.EMPTY;

    public ChunkStatus TargetStatus { get; private set; } = ChunkStatus.EMPTY;

    //Generated whether the chunk reached the target status
    public bool Generated => Chunk is { } chunk && ReferenceEquals(chunk.ChunkStatus, TargetStatus);

    public void SetTargetStatus(ChunkStatus status) => TargetStatus = status;
}
