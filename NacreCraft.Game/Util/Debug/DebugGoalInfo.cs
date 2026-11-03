using NacreCraft.Network;

namespace NacreCraft.Game.Util.Debug;

//DebugGoalInfo goal selector debug payload, maps to vanilla net.minecraft.util.debug.DebugGoalInfo
public sealed record DebugGoalInfo(List<DebugGoalInfo.DebugGoal> Goals)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugGoalInfo> StreamCodec { get; } = new GoalInfoCodec();

    //DebugGoal one goal entry, maps to vanilla DebugGoalInfo.DebugGoal
    public sealed record DebugGoal(int Priority, bool IsRunning, string Name)
    {
        public static StreamCodec<RegistryFriendlyByteBuf, DebugGoal> StreamCodec { get; } = new GoalCodec();
    }
}

internal sealed class GoalInfoCodec : StreamCodec<RegistryFriendlyByteBuf, DebugGoalInfo>
{
    public DebugGoalInfo Decode(RegistryFriendlyByteBuf buf)
    {
        var count = buf.ReadVarInt();
        var goals = new List<DebugGoalInfo.DebugGoal>(Math.Min(count, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < count; i++) goals.Add(DebugGoalInfo.DebugGoal.StreamCodec.Decode(buf));
        return new DebugGoalInfo(goals);
    }

    public void Encode(RegistryFriendlyByteBuf buf, DebugGoalInfo value)
    {
        buf.WriteVarInt(value.Goals.Count);
        foreach (var goal in value.Goals) DebugGoalInfo.DebugGoal.StreamCodec.Encode(buf, goal);
    }
}

internal sealed class GoalCodec : StreamCodec<RegistryFriendlyByteBuf, DebugGoalInfo.DebugGoal>
{
    public DebugGoalInfo.DebugGoal Decode(RegistryFriendlyByteBuf buf)
        => new(buf.ReadVarInt(), buf.ReadBoolean(), buf.ReadString(255));

    public void Encode(RegistryFriendlyByteBuf buf, DebugGoalInfo.DebugGoal value)
    {
        buf.WriteVarInt(value.Priority);
        buf.WriteBoolean(value.IsRunning);
        buf.WriteString(value.Name, 255);
    }
}
