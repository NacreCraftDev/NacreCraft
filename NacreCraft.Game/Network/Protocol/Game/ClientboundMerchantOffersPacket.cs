using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundMerchantOffersPacket merchant offers packet, maps to vanilla ClientboundMerchantOffersPacket
//Fields: ContainerId(int), Offers(List<MerchantOfferData>), VillagerLevel(int), VillagerXp(int), ShowProgress(boolean), CanRestock(boolean)
public sealed record ClientboundMerchantOffersPacket(int ContainerId, object Offers, int VillagerLevel, int VillagerXp, bool ShowProgress, bool CanRestock) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundMerchantOffersPacket> StreamCodec { get; } = new MerchantOffersCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundMerchantOffers;

    public void Handle(ClientGamePacketListener handler) => handler.HandleMerchantOffers(this);

    private sealed class MerchantOffersCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundMerchantOffersPacket>
    {
        //Vanilla MerchantOffer.createFromStream order: buy cost, result stack, optional second cost, then the trade counters
        public ClientboundMerchantOffersPacket Decode(RegistryFriendlyByteBuf buf)
        {
            var containerId = buf.ReadVarInt();
            var count = buf.ReadVarInt();
            var offers = new List<MerchantOfferData>(count);
            for (var i = 0; i < count; i++)
            {
                var baseCost = ReadCost(buf);
                var result = ItemStack.StreamCodec.Decode(buf);
                var secondaryCost = buf.ReadBoolean() ? ReadCost(buf) : null;
                var outOfStock = buf.ReadBoolean();
                var uses = buf.ReadInt();
                var maxUses = buf.ReadInt();
                var xp = buf.ReadInt();
                var specialPriceDiff = buf.ReadInt();
                var priceMultiplier = buf.ReadFloat();
                var demand = buf.ReadInt();
                offers.Add(new MerchantOfferData(baseCost, result, secondaryCost, outOfStock, uses, maxUses, xp, specialPriceDiff, priceMultiplier, demand));
            }
            var villagerLevel = buf.ReadVarInt();
            var villagerXp = buf.ReadVarInt();
            var showProgress = buf.ReadBoolean();
            var canRestock = buf.ReadBoolean();
            return new ClientboundMerchantOffersPacket(containerId, offers, villagerLevel, villagerXp, showProgress, canRestock);
        }

        public void Encode(RegistryFriendlyByteBuf buf, ClientboundMerchantOffersPacket value)
        {
            buf.WriteVarInt(value.ContainerId);
            var offers = value.Offers as IReadOnlyList<MerchantOfferData> ?? Array.Empty<MerchantOfferData>();
            buf.WriteVarInt(offers.Count);
            foreach (var offer in offers)
            {
                WriteCost(buf, offer.BaseCost);
                ItemStack.StreamCodec.Encode(buf, offer.Result);
                buf.WriteBoolean(offer.SecondaryCost is not null);
                if (offer.SecondaryCost is not null) WriteCost(buf, offer.SecondaryCost);
                buf.WriteBoolean(offer.OutOfStock);
                buf.WriteInt(offer.Uses);
                buf.WriteInt(offer.MaxUses);
                buf.WriteInt(offer.Xp);
                buf.WriteInt(offer.SpecialPriceDiff);
                buf.WriteFloat(offer.PriceMultiplier);
                buf.WriteInt(offer.Demand);
            }
            buf.WriteVarInt(value.VillagerLevel);
            buf.WriteVarInt(value.VillagerXp);
            buf.WriteBoolean(value.ShowProgress);
            buf.WriteBoolean(value.CanRestock);
        }

        //ReadCost reads an ItemCost: holder id, count, then the exact component predicate
        private static ItemCostData ReadCost(RegistryFriendlyByteBuf buf)
        {
            var itemId = buf.ReadVarInt();
            var costCount = buf.ReadVarInt();
            var components = DataComponentExactPredicate.StreamCodec.Decode(buf);
            return new ItemCostData(itemId, costCount, components);
        }

        private static void WriteCost(RegistryFriendlyByteBuf buf, ItemCostData cost)
        {
            buf.WriteVarInt(cost.ItemId);
            buf.WriteVarInt(cost.Count);
            DataComponentExactPredicate.StreamCodec.Encode(buf, cost.Components);
        }
    }
}

//ItemCostData the item, count and component requirements of one side of a trade
public sealed record ItemCostData(int ItemId, int Count, DataComponentExactPredicate Components);

//MerchantOfferData one decoded trade offer
public sealed record MerchantOfferData(
    ItemCostData BaseCost,
    ItemStack Result,
    ItemCostData? SecondaryCost,
    bool OutOfStock,
    int Uses,
    int MaxUses,
    int Xp,
    int SpecialPriceDiff,
    float PriceMultiplier,
    int Demand);
