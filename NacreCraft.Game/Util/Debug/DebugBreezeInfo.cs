using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Util.Debug;

//DebugBreezeInfo breeze debug payload, maps to vanilla net.minecraft.util.debug.DebugBreezeInfo
public sealed record DebugBreezeInfo(int? AttackTarget, BlockPos? JumpTarget)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugBreezeInfo> StreamCodec { get; } = new BreezeInfoCodec();
}

internal sealed class BreezeInfoCodec : StreamCodec<RegistryFriendlyByteBuf, DebugBreezeInfo>
{
    public DebugBreezeInfo Decode(RegistryFriendlyByteBuf buf)
    {
        var attackTarget = buf.ReadBoolean() ? buf.ReadVarInt() : (int?)null;
        var jumpTarget = buf.ReadBoolean() ? buf.ReadBlockPos() : (BlockPos?)null;
        return new DebugBreezeInfo(attackTarget, jumpTarget);
    }

    public void Encode(RegistryFriendlyByteBuf buf, DebugBreezeInfo value)
    {
        buf.WriteBoolean(value.AttackTarget.HasValue);
        if (value.AttackTarget.HasValue) buf.WriteVarInt(value.AttackTarget.Value);
        buf.WriteBoolean(value.JumpTarget.HasValue);
        if (value.JumpTarget.HasValue) buf.WriteBlockPos(value.JumpTarget.Value);
    }
}
