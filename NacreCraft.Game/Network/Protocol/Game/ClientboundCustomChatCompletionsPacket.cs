namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundCustomChatCompletionsPacket custom chat completions packet, maps to vanilla ClientboundCustomChatCompletionsPacket
//Fields: Action(Action), Entries(List<String>)
public sealed record ClientboundCustomChatCompletionsPacket(object Action, object Entries) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundCustomChatCompletionsPacket> StreamCodec { get; } = new CustomChatCompletionsCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundCustomChatCompletions;

    public void Handle(ClientGamePacketListener handler) => handler.HandleCustomChatCompletions(this);

    private sealed class CustomChatCompletionsCodec : StreamCodec<FriendlyByteBuf, ClientboundCustomChatCompletionsPacket>
    {
        //Vanilla order: action id (VAR_INT) + collection of strings
        public ClientboundCustomChatCompletionsPacket Decode(FriendlyByteBuf buf)
        {
            var action = buf.ReadVarInt();
            var count = buf.ReadVarInt();
            var entries = new List<string>(count);
            for (var i = 0; i < count; i++) entries.Add(buf.ReadString());
            return new ClientboundCustomChatCompletionsPacket(action, entries);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundCustomChatCompletionsPacket value)
        {
            buf.WriteVarInt(value.Action is int action ? action : 0);
            var entries = value.Entries as IEnumerable<string> ?? Array.Empty<string>();
            var list = entries.ToList();
            buf.WriteVarInt(list.Count);
            foreach (var entry in list) buf.WriteString(entry);
        }
    }
}
