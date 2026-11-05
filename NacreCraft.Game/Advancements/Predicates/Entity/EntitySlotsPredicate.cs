using NacreCraft.Codec;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//EntitySlotsPredicate entity slots predicate, maps to vanilla net.minecraft.advancements.predicates.entity.EntitySlotsPredicate
//Vanilla matches the entity's slot provider; entities have no slot table in NC, so a stated range never matches
public sealed record EntitySlotsPredicate(SlotsPredicate Slots) : EntitySubPredicate
{
    public static readonly Codec<EntitySlotsPredicate> Codec = SlotsPredicate.Codec.ComapFlatMap(
        slots => DataResult<EntitySlotsPredicate>.Success(new EntitySlotsPredicate(slots)),
        predicate => predicate.Slots);

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position)
        => Slots.Slots.Count == 0;
}
