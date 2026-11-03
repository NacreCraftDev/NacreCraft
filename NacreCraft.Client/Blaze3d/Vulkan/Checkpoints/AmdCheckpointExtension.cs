using System.Buffers.Binary;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.AMD;
using NacreCraft.Client.Blaze3d.Buffers;

namespace NacreCraft.Client.Blaze3d.Vulkan.Checkpoints;

//AmdCheckpointExtension uses VK_AMD_buffer_marker, markers land in host visible memory readable at any time
//maps to vanilla com.mojang.blaze3d.vulkan.checkpoints.AmdCheckpointExtension
public sealed class AmdCheckpointExtension : CheckpointExtension
{
    //Top and bottom of the pipeline, the same two stages vanilla writes
    private static readonly PipelineStageFlags2[] Stages =
    {
        PipelineStageFlags2.TopOfPipeBit,
        PipelineStageFlags2.BottomOfPipeBit
    };

    private readonly AmdBufferMarker _markers;
    private readonly List<AmdCheckpointStorage> _storages = new();

    public AmdCheckpointExtension(Vk vk, Instance instance, Device device)
    {
        if (!vk.TryGetDeviceExtension(instance, device, out AmdBufferMarker markers))
            throw new NotSupportedException("The VK_AMD_buffer_marker device extension is unavailable");
        _markers = markers;
    }

    public CheckpointExtension.CheckpointStorage CreateStorage(VulkanDevice device, Queue queue, int maxFramesInFlight)
    {
        var storage = new AmdCheckpointStorage(_markers, device, queue, maxFramesInFlight);
        _storages.Add(storage);
        return storage;
    }

    public IReadOnlyList<CheckpointExtension.QueueCheckpoints> RetrieveCheckpoints(bool isDeviceLost)
    {
        var result = new List<CheckpointExtension.QueueCheckpoints>(_storages.Count);
        foreach (var storage in _storages) result.Add(storage.RetrieveCheckpoints());
        return result;
    }

    public void Dispose()
    {
        foreach (var storage in _storages) storage.Dispose();
    }

    private sealed class AmdCheckpointStorage : AbstractCheckpointStorage, IDisposable
    {
        private readonly AmdBufferMarker _markers;
        private readonly VulkanBuffer _buffer;
        private readonly GpuBufferSlice.MappedView _mappedView;

        public AmdCheckpointStorage(AmdBufferMarker markers, VulkanDevice device, Queue queue, int maxFramesInFlight)
            : base(queue, maxFramesInFlight)
        {
            _markers = markers;
            _buffer = (VulkanBuffer)device.CreateBuffer("Internal marker storage",
                GpuBuffer.UsageMapRead | GpuBuffer.UsageCopyDst, Stages.Length * sizeof(int));
            _mappedView = _buffer.Map(true, false);
        }

        protected override void RecordCheckpointCore(CommandBuffer commandBuffer, int id)
        {
            for (var i = 0; i < Stages.Length; i++)
                _markers.CmdWriteBufferMarker2(commandBuffer, Stages[i], _buffer.Handle, (ulong)(i * sizeof(int)), (uint)id);
        }

        public CheckpointExtension.QueueCheckpoints RetrieveCheckpoints()
        {
            var stageCheckpoints = new List<CheckpointExtension.StageCheckpoint>(Stages.Length);
            for (var i = 0; i < Stages.Length; i++)
            {
                var marker = BinaryPrimitives.ReadInt32LittleEndian(_mappedView.Data.Slice(i * sizeof(int), sizeof(int)));
                var checkpoint = FindCheckpoint(marker);
                if (checkpoint is null) continue;
                stageCheckpoints.Add(new CheckpointExtension.StageCheckpoint(
                    (ulong)Stages[i], checkpoint.Value.Type, checkpoint.Value.Label));
            }
            return new CheckpointExtension.QueueCheckpoints((ulong)Queue.Handle, stageCheckpoints);
        }

        public void Dispose()
        {
            _mappedView.Dispose();
            _buffer.Dispose();
        }
    }
}
