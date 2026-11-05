using NacreCraft.Registry;
using NacreCraft.Storage.Chunk;

namespace NacreCraft.Storage;

//ChunkGenerationTask one chunk's ascent to a target status, maps to vanilla net.minecraft.server.level.ChunkGenerationTask
//The task scheduler creates it when a ticket asks for a status the chunk does not have yet
public sealed class ChunkGenerationTask
{
    public ChunkGenerationTask(ChunkHolder holder, ChunkStatus targetStatus)
    {
        Holder = holder;
        TargetStatus = targetStatus;
    }

    public ChunkHolder Holder { get; }

    public ChunkStatus TargetStatus { get; }

    //Started whether the task already handed work to a stage
    public bool Started { get; private set; }

    //MarkStarted records that the task began, maps to vanilla the started flag
    public void MarkStarted() => Started = true;

    //IsComplete whether the chunk reached the target status
    //The status chain has no public index in NC, so completion is an exact status match
    public bool IsComplete => Holder.Chunk is { } chunk && ReferenceEquals(chunk.ChunkStatus, TargetStatus);
}
