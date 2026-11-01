using NacreCraft.Registry;

namespace NacreCraft.Storage;

//One chunk waiting to advance one status; the scheduler requeues it after each stage
public sealed class ChunkStageTask
{
    public ChunkAccess Chunk { get; }

    //Holder owns the stage timing the GUI reads and carries the priority the queue is ordered by
    public ChunkHolder Holder { get; }

    //Status is the stage this task will run next
    public ChunkStatus Status { get; set; }

    public TaskCompletionSource<ChunkAccess> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ChunkStageTask(ChunkAccess chunk, ChunkStatus status, ChunkHolder holder)
    {
        Chunk = chunk;
        Status = status;
        Holder = holder;
    }
}
