using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.NV;

namespace NacreCraft.Client.Blaze3d.Vulkan.Checkpoints;

//NvidiaCheckpointExtension uses VK_NV_device_diagnostic_checkpoints, markers are readable after a device loss
//maps to vanilla com.mojang.blaze3d.vulkan.checkpoints.NvidiaCheckpointExtension
public sealed unsafe class NvidiaCheckpointExtension : CheckpointExtension
{
    private readonly NVDeviceDiagnosticCheckpoints _checkpoints;
    private readonly List<NvidiaCheckpointStorage> _storages = new();

    public NvidiaCheckpointExtension(Vk vk, Instance instance, Device device)
    {
        if (!vk.TryGetDeviceExtension(instance, device, out NVDeviceDiagnosticCheckpoints checkpoints))
            throw new NotSupportedException("The VK_NV_device_diagnostic_checkpoints device extension is unavailable");
        _checkpoints = checkpoints;
    }

    public CheckpointExtension.CheckpointStorage CreateStorage(VulkanDevice device, Queue queue, int maxFramesInFlight)
    {
        var storage = new NvidiaCheckpointStorage(_checkpoints, queue, maxFramesInFlight);
        _storages.Add(storage);
        return storage;
    }

    public IReadOnlyList<CheckpointExtension.QueueCheckpoints> RetrieveCheckpoints(bool isDeviceLost)
    {
        //NV checkpoints beyond the driver queue only survive a lost device
        if (!isDeviceLost) return Array.Empty<CheckpointExtension.QueueCheckpoints>();
        var result = new List<CheckpointExtension.QueueCheckpoints>(_storages.Count);
        foreach (var storage in _storages) result.Add(storage.RetrieveCheckpoints());
        return result;
    }

    public void Dispose()
    {
    }

    private sealed unsafe class NvidiaCheckpointStorage : AbstractCheckpointStorage
    {
        private readonly NVDeviceDiagnosticCheckpoints _checkpoints;

        public NvidiaCheckpointStorage(NVDeviceDiagnosticCheckpoints checkpoints, Queue queue, int maxFramesInFlight)
            : base(queue, maxFramesInFlight)
        {
            _checkpoints = checkpoints;
        }

        protected override void RecordCheckpointCore(CommandBuffer commandBuffer, int id)
            => _checkpoints.CmdSetCheckpoint(commandBuffer, (void*)(nint)id);

        public CheckpointExtension.QueueCheckpoints RetrieveCheckpoints()
        {
            var stageCheckpoints = new List<CheckpointExtension.StageCheckpoint>();
            uint count = 0;
            _checkpoints.GetQueueCheckpointData2(Queue, &count, null);
            if (count == 0) return new CheckpointExtension.QueueCheckpoints((ulong)Queue.Handle, stageCheckpoints);

            var data = stackalloc CheckpointData2NV[(int)count];
            for (var i = 0; i < count; i++) data[i].SType = StructureType.CheckpointData2NV;
            _checkpoints.GetQueueCheckpointData2(Queue, &count, data);
            for (var i = 0; i < count; i++)
            {
                var checkpoint = FindCheckpoint((int)(long)data[i].PCheckpointMarker);
                if (checkpoint is null) continue;
                stageCheckpoints.Add(new CheckpointExtension.StageCheckpoint(
                    (ulong)data[i].Stage, checkpoint.Value.Type, checkpoint.Value.Label));
            }
            return new CheckpointExtension.QueueCheckpoints((ulong)Queue.Handle, stageCheckpoints);
        }
    }
}
