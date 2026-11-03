using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Util.Debug;

namespace NacreCraft.Game.Util.DebugChart;

//RemoteSampleLogger sample logger that pushes debug chart samples to subscribed players, maps to vanilla net.minecraft.util.debugchart.RemoteSampleLogger
public sealed class RemoteSampleLogger : AbstractSampleLogger
{
    private readonly ServerDebugSubscribers _subscribers;
    private readonly RemoteDebugSampleType _sampleType;

    public RemoteSampleLogger(int dimensions, ServerDebugSubscribers subscribers, RemoteDebugSampleType sampleType)
        : this(dimensions, subscribers, sampleType, new long[dimensions]) { }

    public RemoteSampleLogger(int dimensions, ServerDebugSubscribers subscribers, RemoteDebugSampleType sampleType, long[] defaults)
        : base(dimensions, defaults)
    {
        _subscribers = subscribers;
        _sampleType = sampleType;
    }

    //UseSample broadcasts the finished sample frame to every subscriber of this sample type
    protected override void UseSample()
    {
        var subscription = _sampleType.Subscription();
        if (_subscribers.HasAnySubscriberFor(subscription))
            _subscribers.BroadcastToAll(subscription, new ClientboundDebugSamplePacket((long[])Sample.Clone(), _sampleType));
    }
}
