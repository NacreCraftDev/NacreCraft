namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundSetPlayerTeamPacket player team packet, maps to vanilla ClientboundSetPlayerTeamPacket
//Fields: Method(int), Name(String), Players(Collection<String>), Parameters(Optional<Parameters>)
public sealed record ClientboundSetPlayerTeamPacket(int Method, string Name, object Players, object Parameters) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundSetPlayerTeamPacket> StreamCodec { get; } = new SetPlayerTeamCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundSetPlayerTeam;

    public void Handle(ClientGamePacketListener handler) => handler.HandleSetPlayerTeamPacket(this);

    private sealed class SetPlayerTeamCodec : StreamCodec<FriendlyByteBuf, ClientboundSetPlayerTeamPacket>
    {
        //Vanilla order: name, method byte, optional parameters (add/change), optional player list (add/join/leave)
        public ClientboundSetPlayerTeamPacket Decode(FriendlyByteBuf buf)
        {
            var name = buf.ReadString();
            var method = buf.ReadByte();
            object? parameters = null;
            if (method is 0 or 2)
            {
                var displayName = ComponentSerialization.StreamCodec.Decode(buf);
                var prefix = ComponentSerialization.StreamCodec.Decode(buf);
                var suffix = ComponentSerialization.StreamCodec.Decode(buf);
                var nameTagVisibility = buf.ReadVarInt();
                var collisionRule = buf.ReadVarInt();
                var color = buf.ReadBoolean() ? buf.ReadVarInt() : -1;
                var options = buf.ReadByte();
                parameters = (displayName, prefix, suffix, nameTagVisibility, collisionRule, color, options);
            }
            var players = new List<string>();
            if (method is 0 or 3 or 4)
            {
                var count = buf.ReadVarInt();
                for (var i = 0; i < count; i++) players.Add(buf.ReadString());
            }
            return new ClientboundSetPlayerTeamPacket(method, name, players, parameters!);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundSetPlayerTeamPacket value)
        {
            buf.WriteString(value.Name);
            buf.WriteByte((byte)value.Method);
            if (value.Method is 0 or 2)
            {
                //Parameters are decoded as a tuple; a missing tuple falls back to an empty team definition
                var displayName = CommonComponents.Empty;
                var prefix = CommonComponents.Empty;
                var suffix = CommonComponents.Empty;
                var visibility = 0;
                var collision = 0;
                var color = -1;
                var options = (byte)0;
                if (value.Parameters is ValueTuple<Component, Component, Component, int, int, int, byte> parameters)
                {
                    displayName = parameters.Item1;
                    prefix = parameters.Item2;
                    suffix = parameters.Item3;
                    visibility = parameters.Item4;
                    collision = parameters.Item5;
                    color = parameters.Item6;
                    options = parameters.Item7;
                }
                ComponentSerialization.StreamCodec.Encode(buf, displayName);
                ComponentSerialization.StreamCodec.Encode(buf, prefix);
                ComponentSerialization.StreamCodec.Encode(buf, suffix);
                buf.WriteVarInt(visibility);
                buf.WriteVarInt(collision);
                buf.WriteBoolean(color >= 0);
                if (color >= 0) buf.WriteVarInt(color);
                buf.WriteByte(options);
            }
            if (value.Method is 0 or 3 or 4)
            {
                var players = value.Players as IEnumerable<string> ?? Array.Empty<string>();
                var list = players.ToList();
                buf.WriteVarInt(list.Count);
                foreach (var player in list) buf.WriteString(player);
            }
        }
    }
}
