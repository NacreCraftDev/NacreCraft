using NacreCraft.Network;

namespace NacreCraft.Game.Util;

//Unit single-value marker type, maps to vanilla net.minecraft.util.Unit
//Used where a debug subscription only signals presence, so nothing is written to the wire
public readonly record struct Unit
{
    public static Unit Instance => default;

    public static StreamCodec<RegistryFriendlyByteBuf, Unit> StreamCodec { get; } = new UnitCodec();
}

internal sealed class UnitCodec : StreamCodec<RegistryFriendlyByteBuf, Unit>
{
    public Unit Decode(RegistryFriendlyByteBuf buf) => Unit.Instance;

    public void Encode(RegistryFriendlyByteBuf buf, Unit value) { }
}
