namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundRecipeBookRemovePacket recipe book remove packet, maps to vanilla ClientboundRecipeBookRemovePacket
//Field: Recipes(List<int> recipe display ids)
public sealed record ClientboundRecipeBookRemovePacket(object Recipes) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundRecipeBookRemovePacket> StreamCodec { get; } = new RecipeBookRemoveCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundRecipeBookRemove;

    public void Handle(ClientGamePacketListener handler) => handler.HandleRecipeBookRemove(this);

    private sealed class RecipeBookRemoveCodec : StreamCodec<FriendlyByteBuf, ClientboundRecipeBookRemovePacket>
    {
        //RecipeDisplayId is a bare VAR_INT index
        public ClientboundRecipeBookRemovePacket Decode(FriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var recipes = new List<int>(count);
            for (var i = 0; i < count; i++) recipes.Add(buf.ReadVarInt());
            return new ClientboundRecipeBookRemovePacket(recipes);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundRecipeBookRemovePacket value)
        {
            var recipes = value.Recipes as IReadOnlyList<int> ?? Array.Empty<int>();
            buf.WriteVarInt(recipes.Count);
            foreach (var recipe in recipes) buf.WriteVarInt(recipe);
        }
    }
}
