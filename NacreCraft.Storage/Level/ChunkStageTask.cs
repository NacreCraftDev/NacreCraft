using NacreCraft.Registry;

namespace NacreCraft.Storage;

//One chunk waiting to advance one status; the scheduler requeues it after each stage
public sealed class ChunkStageTask
{
    public ChunkAccess Chunk { get; }

    //Holder owns the stage timing the GUI reads
    public ChunkHolder Holder { get; }

    //Status is the stage this task will run next
    public ChunkStatus Status { get; set; }

    //Lower values run first, matching the ticket level ordering
    public int Priority { get; }

    public TaskCompletionSource<ChunkAccess> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ChunkStageTask(ChunkAccess chunk, ChunkStatus status, int priority, ChunkHolder holder)
    {
        Chunk = chunk;
        Status = status;
        Priority = priority;
        Holder = holder;
    }
}
