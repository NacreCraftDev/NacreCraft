using NacreCraft.Logging;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundRecipeBookAddPacket recipe book add packet, maps to vanilla ClientboundRecipeBookAddPacket
//Entries keeps the raw payload because RecipeDisplayEntry needs the slot display and recipe display codecs, which are not
//implemented yet; Replace is part of that payload and therefore stays false until the entries can be decoded
public sealed record ClientboundRecipeBookAddPacket(object Entries, bool Replace) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundRecipeBookAddPacket> StreamCodec { get; } = new RecipeBookAddCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundRecipeBookAdd;

    public void Handle(ClientGamePacketListener handler) => handler.HandleRecipeBookAdd(this);

    private sealed class RecipeBookAddCodec : StreamCodec<FriendlyByteBuf, ClientboundRecipeBookAddPacket>
    {
        public ClientboundRecipeBookAddPacket Decode(FriendlyByteBuf buf)
        {
            var payload = buf.ReadBytes(buf.ReadableBytes);
            Log.Debug($"RecipeBookAdd kept {payload.Length} bytes, recipe display codecs are not implemented");
            return new ClientboundRecipeBookAddPacket(payload, false);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundRecipeBookAddPacket value)
        {
            if (value.Entries is byte[] payload) buf.WriteBytes(payload);
        }
    }
}
