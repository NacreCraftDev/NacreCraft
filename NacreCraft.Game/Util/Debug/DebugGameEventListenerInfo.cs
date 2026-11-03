using NacreCraft.Network;

namespace NacreCraft.Game.Util.Debug;

//DebugGameEventListenerInfo game event listener debug payload, maps to vanilla net.minecraft.util.debug.DebugGameEventListenerInfo
public sealed record DebugGameEventListenerInfo(int ListenerRadius)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugGameEventListenerInfo> StreamCodec { get; } = new GameEventListenerInfoCodec();
}

internal sealed class GameEventListenerInfoCodec : StreamCodec<RegistryFriendlyByteBuf, DebugGameEventListenerInfo>
{
    public DebugGameEventListenerInfo Decode(RegistryFriendlyByteBuf buf) => new(buf.ReadVarInt());

    public void Encode(RegistryFriendlyByteBuf buf, DebugGameEventListenerInfo value) => buf.WriteVarInt(value.ListenerRadius);
}
