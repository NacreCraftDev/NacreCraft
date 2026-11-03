using NacreCraft.Network;
using NacreCraft.Registry;

namespace NacreCraft.Game.Util.Debug;

//DebugHiveInfo beehive debug payload, maps to vanilla net.minecraft.util.debug.DebugHiveInfo
//The vanilla pack(BeehiveBlockEntity) helper is omitted until the beehive block entity exists in NC
public sealed record DebugHiveInfo(Block Type, int OccupantCount, int HoneyLevel, bool Sedated)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugHiveInfo> StreamCodec { get; } = new HiveInfoCodec();
}

internal sealed class HiveInfoCodec : StreamCodec<RegistryFriendlyByteBuf, DebugHiveInfo>
{
    public DebugHiveInfo Decode(RegistryFriendlyByteBuf buf)
    {
        var id = buf.ReadVarInt();
        var type = BuiltInRegistries.BLOCK.ById(id)
            ?? throw new InvalidOperationException($"unknown block id {id}");
        return new DebugHiveInfo(type, buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadBoolean());
    }

    public void Encode(RegistryFriendlyByteBuf buf, DebugHiveInfo value)
    {
        var id = BuiltInRegistries.BLOCK.GetId(value.Type);
        if (id == IdMap<Block>.Default)
            throw new InvalidOperationException($"block not registered: {value.Type}");
        buf.WriteVarInt(id);
        buf.WriteVarInt(value.OccupantCount);
        buf.WriteVarInt(value.HoneyLevel);
        buf.WriteBoolean(value.Sedated);
    }
}
