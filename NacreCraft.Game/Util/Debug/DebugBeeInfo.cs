using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Util.Debug;

//DebugBeeInfo bee debug payload, maps to vanilla net.minecraft.util.debug.DebugBeeInfo
public sealed record DebugBeeInfo(BlockPos? HivePos, BlockPos? FlowerPos, int TravelTicks, List<BlockPos> BlacklistedHives)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugBeeInfo> StreamCodec { get; } = new BeeInfoCodec();

    public bool HasHive(BlockPos hivePos) => HivePos is { } pos && pos.Equals(hivePos);
}

internal sealed class BeeInfoCodec : StreamCodec<RegistryFriendlyByteBuf, DebugBeeInfo>
{
    public DebugBeeInfo Decode(RegistryFriendlyByteBuf buf)
    {
        var hivePos = ReadOptionalPos(buf);
        var flowerPos = ReadOptionalPos(buf);
        var travelTicks = buf.ReadVarInt();
        var count = buf.ReadVarInt();
        var blacklisted = new List<BlockPos>(Math.Min(count, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < count; i++) blacklisted.Add(buf.ReadBlockPos());
        return new DebugBeeInfo(hivePos, flowerPos, travelTicks, blacklisted);
    }

    public void Encode(RegistryFriendlyByteBuf buf, DebugBeeInfo value)
    {
        WriteOptionalPos(buf, value.HivePos);
        WriteOptionalPos(buf, value.FlowerPos);
        buf.WriteVarInt(value.TravelTicks);
        buf.WriteVarInt(value.BlacklistedHives.Count);
        foreach (var pos in value.BlacklistedHives) buf.WriteBlockPos(pos);
    }

    //ReadOptionalPos optional block position, maps to vanilla BlockPos.STREAM_CODEC.apply(ByteBufCodecs::optional)
    private static BlockPos? ReadOptionalPos(RegistryFriendlyByteBuf buf)
        => buf.ReadBoolean() ? buf.ReadBlockPos() : null;

    private static void WriteOptionalPos(RegistryFriendlyByteBuf buf, BlockPos? pos)
    {
        buf.WriteBoolean(pos.HasValue);
        if (pos.HasValue) buf.WriteBlockPos(pos.Value);
    }
}
