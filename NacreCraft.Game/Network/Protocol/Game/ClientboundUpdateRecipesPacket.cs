using NacreCraft.Logging;
using NacreCraft.Registry;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundUpdateRecipesPacket recipes update packet, maps to vanilla ClientboundUpdateRecipesPacket
//ItemSets holds List<RecipePropertySetData>; StonecutterRecipes keeps the remaining bytes verbatim because slot display
//codecs are not implemented yet, and the property sets are all the client needs before the recipe book exists
public sealed record ClientboundUpdateRecipesPacket(object ItemSets, object StonecutterRecipes) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundUpdateRecipesPacket> StreamCodec { get; } = new UpdateRecipesCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundUpdateRecipes;

    public void Handle(ClientGamePacketListener handler) => handler.HandleUpdateRecipes(this);

    private sealed class UpdateRecipesCodec : StreamCodec<FriendlyByteBuf, ClientboundUpdateRecipesPacket>
    {
        public ClientboundUpdateRecipesPacket Decode(FriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var itemSets = new List<RecipePropertySetData>(count);
            for (var i = 0; i < count; i++)
            {
                var key = buf.ReadIdentifier();
                //RecipePropertySet is a list of item registry ids
                var itemCount = buf.ReadVarInt();
                var items = new List<int>(itemCount);
                for (var j = 0; j < itemCount; j++) items.Add(buf.ReadVarInt());
                itemSets.Add(new RecipePropertySetData(key, items));
            }
            var stonecutter = buf.ReadBytes(buf.ReadableBytes);
            Log.Debug($"UpdateRecipes kept {stonecutter.Length} stonecutter bytes, slot display codecs are not implemented");
            return new ClientboundUpdateRecipesPacket(itemSets, stonecutter);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundUpdateRecipesPacket value)
        {
            var itemSets = value.ItemSets as IReadOnlyList<RecipePropertySetData> ?? Array.Empty<RecipePropertySetData>();
            buf.WriteVarInt(itemSets.Count);
            foreach (var set in itemSets)
            {
                buf.WriteIdentifier(set.Key);
                buf.WriteVarInt(set.ItemIds.Count);
                foreach (var item in set.ItemIds) buf.WriteVarInt(item);
            }
            if (value.StonecutterRecipes is byte[] stonecutter) buf.WriteBytes(stonecutter);
        }
    }
}

//RecipePropertySetData one property set keyed by its registry identifier
public sealed record RecipePropertySetData(Identifier Key, List<int> ItemIds);
