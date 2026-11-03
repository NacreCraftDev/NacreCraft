using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Util.Debug;

//DebugGameEventInfo game event debug payload, maps to vanilla net.minecraft.util.debug.DebugGameEventInfo
public sealed record DebugGameEventInfo(Holder<GameEvent> Event, Vec3 Pos)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugGameEventInfo> StreamCodec { get; } = new GameEventInfoCodec();
}

internal sealed class GameEventInfoCodec : StreamCodec<RegistryFriendlyByteBuf, DebugGameEventInfo>
{
    private static readonly StreamCodec<RegistryFriendlyByteBuf, Holder<GameEvent>> HolderCodec =
        ByteBufCodecs.Holder(Registries.GAME_EVENT);

    public DebugGameEventInfo Decode(RegistryFriendlyByteBuf buf)
        => new(HolderCodec.Decode(buf), new Vec3(buf.ReadDouble(), buf.ReadDouble(), buf.ReadDouble()));

    public void Encode(RegistryFriendlyByteBuf buf, DebugGameEventInfo value)
    {
        HolderCodec.Encode(buf, value.Event);
        buf.WriteDouble(value.Pos.X);
        buf.WriteDouble(value.Pos.Y);
        buf.WriteDouble(value.Pos.Z);
    }
}
