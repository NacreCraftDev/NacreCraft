using NacreCraft.Registry;

namespace NacreCraft.Network;

//ItemCodecs network codecs for Item, maps to the vanilla Item.STREAM_CODEC static field
//Vanilla Item.STREAM_CODEC lives in the Item class; since NacreCraft's Registry does not depend on Network, it goes in the Network sublibrary
//HolderCodec reads an id from RegistryFriendlyByteBuf and turns it into Holder<Item>
public static class ItemCodecs
{
    //StreamCodec codec for Holder<Item> using ByteBufCodecs.Holder to look up the ITEM registry
    public static readonly StreamCodec<RegistryFriendlyByteBuf, Holder<Item>> StreamCodec
        = ByteBufCodecs.Holder(Registries.ITEM);
}
