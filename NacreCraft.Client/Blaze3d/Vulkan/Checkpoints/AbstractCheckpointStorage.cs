using Silk.NET.Vulkan;

namespace NacreCraft.Client.Blaze3d.Vulkan.Checkpoints;

//AbstractCheckpointStorage keeps the recorded labels per frame and maps a read back marker id to its checkpoint
//maps to vanilla com.mojang.blaze3d.vulkan.checkpoints.AbstractCheckpointStorage
internal abstract class AbstractCheckpointStorage : CheckpointExtension.CheckpointStorage
{
    protected readonly Queue Queue;
    private readonly int _maxFramesInFlight;
    private readonly List<Checkpoint>[] _checkpointsByFrame;
    private int _frame;
    private int _nextCheckpointId;

    protected AbstractCheckpointStorage(Queue queue, int maxFramesInFlight)
    {
        Queue = queue;
        _maxFramesInFlight = maxFramesInFlight;
        _checkpointsByFrame = new List<Checkpoint>[maxFramesInFlight];
        for (var i = 0; i < maxFramesInFlight; i++) _checkpointsByFrame[i] = new List<Checkpoint>();
    }

    public void Rotate()
    {
        _frame = (_frame + 1) % _maxFramesInFlight;
        _checkpointsByFrame[_frame].Clear();
    }

    public void RecordCheckpoint(CommandBuffer commandBuffer, CheckpointExtension.CheckpointType type, Func<string> label)
    {
        var id = _nextCheckpointId++;
        _checkpointsByFrame[_frame].Add(new Checkpoint(id, label(), type));
        RecordCheckpointCore(commandBuffer, id);
    }

    //RecordCheckpointCore writes the backend specific marker for the id
    protected abstract void RecordCheckpointCore(CommandBuffer commandBuffer, int id);

    //FindCheckpoint resolves a marker id back to its label, null when the frame slot was already rotated out
    protected Checkpoint? FindCheckpoint(int id)
    {
        foreach (var frame in _checkpointsByFrame)
            foreach (var checkpoint in frame)
                if (checkpoint.Id == id) return checkpoint;
        return null;
    }

    protected readonly record struct Checkpoint(int Id, string Label, CheckpointExtension.CheckpointType Type);
}
