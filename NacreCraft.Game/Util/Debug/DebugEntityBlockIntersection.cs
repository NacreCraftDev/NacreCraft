using NacreCraft.Network;

namespace NacreCraft.Game.Util.Debug;

//DebugEntityBlockIntersection entity/block intersection debug value, maps to vanilla net.minecraft.util.debug.DebugEntityBlockIntersection
public enum DebugEntityBlockIntersection
{
    InBlock = 0,
    InFluid = 1,
    InAir = 2
}

//DebugEntityBlockIntersectionCodec id-mapped codec, maps to vanilla DebugEntityBlockIntersection.STREAM_CODEC
public sealed class DebugEntityBlockIntersectionCodec : StreamCodec<RegistryFriendlyByteBuf, DebugEntityBlockIntersection>
{
    public static DebugEntityBlockIntersectionCodec Instance { get; } = new();

    public DebugEntityBlockIntersection Decode(RegistryFriendlyByteBuf buf) => (DebugEntityBlockIntersection)buf.ReadVarInt();

    public void Encode(RegistryFriendlyByteBuf buf, DebugEntityBlockIntersection value) => buf.WriteVarInt((int)value);
}

//DebugEntityBlockIntersectionExtensions overlay colors, maps to the vanilla color table
public static class DebugEntityBlockIntersectionExtensions
{
    public static int Color(this DebugEntityBlockIntersection value) => value switch
    {
        DebugEntityBlockIntersection.InBlock => 0x6000FF00,
        DebugEntityBlockIntersection.InFluid => 0x600000FF,
        _ => 0x60333333
    };
}
