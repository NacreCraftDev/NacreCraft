using NacreCraft.Logging;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundPlaceGhostRecipePacket place ghost recipe packet, maps to vanilla ClientboundPlaceGhostRecipePacket
//Fields: ContainerId(int), RecipeDisplay(raw bytes)
public sealed record ClientboundPlaceGhostRecipePacket(int ContainerId, object RecipeDisplay) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundPlaceGhostRecipePacket> StreamCodec { get; } = new PlaceGhostRecipeCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundPlaceGhostRecipe;

    public void Handle(ClientGamePacketListener handler) => handler.HandlePlaceRecipe(this);

    private sealed class PlaceGhostRecipeCodec : StreamCodec<FriendlyByteBuf, ClientboundPlaceGhostRecipePacket>
    {
        //RecipeDisplay dispatches through the slot display codecs, which are not implemented yet, so the display is kept verbatim
        public ClientboundPlaceGhostRecipePacket Decode(FriendlyByteBuf buf)
        {
            var containerId = buf.ReadVarInt();
            var display = buf.ReadBytes(buf.ReadableBytes);
            Log.Debug($"PlaceGhostRecipe kept {display.Length} bytes, recipe display codecs are not implemented");
            return new ClientboundPlaceGhostRecipePacket(containerId, display);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundPlaceGhostRecipePacket value)
        {
            buf.WriteVarInt(value.ContainerId);
            if (value.RecipeDisplay is byte[] display) buf.WriteBytes(display);
        }
    }
}
