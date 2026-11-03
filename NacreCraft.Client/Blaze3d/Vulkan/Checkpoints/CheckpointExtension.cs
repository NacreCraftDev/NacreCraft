using Silk.NET.Vulkan;

namespace NacreCraft.Client.Blaze3d.Vulkan.Checkpoints;

//CheckpointExtension records GPU checkpoints so a device loss can report which pass the GPU died in
//maps to vanilla com.mojang.blaze3d.vulkan.checkpoints.CheckpointExtension
public interface CheckpointExtension : IDisposable
{
    CheckpointStorage CreateStorage(VulkanDevice device, Queue queue, int maxFramesInFlight);

    //RetrieveCheckpoints reads the markers left by the GPU, isDeviceLost selects the slower path that still works after a hang
    IReadOnlyList<QueueCheckpoints> RetrieveCheckpoints(bool isDeviceLost);

    //StageCheckpoint one pipeline stage and the pass label it was executing
    public readonly record struct StageCheckpoint(ulong Stage, CheckpointType Type, string Label);

    //QueueCheckpoints the checkpoints read back from one queue
    public sealed record QueueCheckpoints(ulong Queue, IReadOnlyList<StageCheckpoint> Checkpoints);

    public enum CheckpointType
    {
        BeginRenderPass,
        EndRenderPass
    }

    public interface CheckpointStorage
    {
        //Rotate advances to the next frame slot and drops the oldest recorded labels
        void Rotate();

        //RecordCheckpoint writes a marker into the command buffer; label is only evaluated when a marker is written
        void RecordCheckpoint(CommandBuffer commandBuffer, CheckpointType type, Func<string> label);
    }
}
