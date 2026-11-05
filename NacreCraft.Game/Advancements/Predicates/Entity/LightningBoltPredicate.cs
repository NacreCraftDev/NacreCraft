using NacreCraft.Codec;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//LightningBoltPredicate lightning bolt predicate, maps to vanilla net.minecraft.advancements.predicates.entity.LightningBoltPredicate
//Checks the blocks set on fire and the struck entity; the lightning bolt entity is not ported
public sealed record LightningBoltPredicate(MinMaxBounds.Ints BlocksSetOnFire,
    Optional<EntityPredicate> EntityStruck) : EntitySubPredicate
{
    public static readonly Codec<LightningBoltPredicate> Codec = RecordCodecBuilder.Of2(
        MinMaxBounds.Ints.CODEC.OptionalFieldOf("blocks_set_on_fire", MinMaxBounds.Ints.Any)
            .ForGetter((LightningBoltPredicate predicate) => predicate.BlocksSetOnFire),
        EntityPredicate.Codec.OptionalFieldOf("entity_struck")
            .ForGetter((LightningBoltPredicate predicate) => predicate.EntityStruck),
        (blocksSetOnFire, entityStruck) => new LightningBoltPredicate(blocksSetOnFire, entityStruck));

    public static LightningBoltPredicate BlockSetOnFire(MinMaxBounds.Ints count)
        => new(count, Optional<EntityPredicate>.Empty());

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position) => false;
}
