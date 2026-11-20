using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Component;

//SeededContainerLoot the loot table and seed of a container item, maps to vanilla net.minecraft.world.item.component.SeededContainerLoot
public sealed record SeededContainerLoot(ResourceKey<LootTable> LootTable, long Seed)
{
    private static readonly Codec<ResourceKey<LootTable>> LootTableKeyCodec =
        ItemComponentCodecs.IdentifierCodec.Xmap(
            id => ResourceKey<LootTable>.Create(Registries.LOOT_TABLE, id),
            key => key.Identifier);

    private static readonly StreamCodec<FriendlyByteBuf, ResourceKey<LootTable>> LootTableKeyStreamCodec =
        ItemComponentCodecs.IdentifierStreamCodec.Map(
            id => ResourceKey<LootTable>.Create(Registries.LOOT_TABLE, id),
            key => key.Identifier);

    public static readonly Codec<SeededContainerLoot> Codec = RecordCodecBuilder.Of2(
        LootTableKeyCodec.FieldOf("loot_table").ForGetter((SeededContainerLoot value) => value.LootTable),
        Codecs.Long.OptionalFieldOf("seed", 0L).ForGetter((SeededContainerLoot value) => value.Seed),
        (table, seed) => new SeededContainerLoot(table, seed));

    public static readonly StreamCodec<FriendlyByteBuf, SeededContainerLoot> StreamCodec = StreamCodecs.Composite(
        LootTableKeyStreamCodec, value => value.LootTable,
        ByteBufCodecs.Long, value => value.Seed,
        (table, seed) => new SeededContainerLoot(table, seed));
}
