using NacreCraft.Game.Util.Debug;

namespace NacreCraft.Game.Util.DebugChart;

//RemoteDebugSampleType debug chart samples pushed to subscribed clients, maps to vanilla net.minecraft.util.debugchart.RemoteDebugSampleType
//The ordinal is what the network enum codec writes
public enum RemoteDebugSampleType
{
    TickTime = 0
}

public static class RemoteDebugSampleTypeExtensions
{
    public static DebugSubscription Subscription(this RemoteDebugSampleType type)
        => type switch
        {
            RemoteDebugSampleType.TickTime => DebugSubscriptions.DedicatedServerTickTime,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "unknown remote debug sample type")
        };
}
