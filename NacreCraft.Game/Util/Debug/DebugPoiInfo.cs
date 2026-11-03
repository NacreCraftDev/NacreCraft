using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.Debug;

//DebugPoiInfo point of interest debug payload, maps to vanilla net.minecraft.util.debug.DebugPoiInfo
public sealed record DebugPoiInfo(BlockPos Pos, Holder<PoiType> PoiType, int FreeTicketCount)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugPoiInfo> StreamCodec { get; } = new PoiInfoCodec();

    //PoiRecord constructor maps to the vanilla DebugPoiInfo(PoiRecord) constructor
    //SimplePoiManager tracks occupancy only, so the free ticket count is always zero
    public DebugPoiInfo(PoiRecord record)
        : this(record.Pos, BuiltInRegistries.POINT_OF_INTEREST_TYPE.WrapAsHolder(record.Type), 0) { }
}

internal sealed class PoiInfoCodec : StreamCodec<RegistryFriendlyByteBuf, DebugPoiInfo>
{
    private static readonly StreamCodec<RegistryFriendlyByteBuf, Holder<PoiType>> HolderCodec =
        ByteBufCodecs.Holder(Registries.POINT_OF_INTEREST_TYPE);

    public DebugPoiInfo Decode(RegistryFriendlyByteBuf buf)
        => new(buf.ReadBlockPos(), HolderCodec.Decode(buf), buf.ReadVarInt());

    public void Encode(RegistryFriendlyByteBuf buf, DebugPoiInfo value)
    {
        buf.WriteBlockPos(value.Pos);
        HolderCodec.Encode(buf, value.PoiType);
        buf.WriteVarInt(value.FreeTicketCount);
    }
}
