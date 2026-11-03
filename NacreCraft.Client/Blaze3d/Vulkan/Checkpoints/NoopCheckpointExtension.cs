using Silk.NET.Vulkan;

namespace NacreCraft.Client.Blaze3d.Vulkan.Checkpoints;

//NoopCheckpointExtension used when the GPU exposes neither marker extension
//maps to vanilla com.mojang.blaze3d.vulkan.checkpoints.NoopCheckpointExtension
public sealed class NoopCheckpointExtension : CheckpointExtension
{
    public static readonly NoopCheckpointExtension Instance = new();

    private NoopCheckpointExtension() { }

    public CheckpointExtension.CheckpointStorage CreateStorage(VulkanDevice device, Queue queue, int maxFramesInFlight)
        => NoopCheckpointStorage.Instance;

    public IReadOnlyList<CheckpointExtension.QueueCheckpoints> RetrieveCheckpoints(bool isDeviceLost)
        => Array.Empty<CheckpointExtension.QueueCheckpoints>();

    public void Dispose() { }

    private sealed class NoopCheckpointStorage : CheckpointExtension.CheckpointStorage
    {
        public static readonly NoopCheckpointStorage Instance = new();

        private NoopCheckpointStorage() { }

        public void Rotate() { }

        public void RecordCheckpoint(CommandBuffer commandBuffer, CheckpointExtension.CheckpointType type, Func<string> label) { }
    }
}
