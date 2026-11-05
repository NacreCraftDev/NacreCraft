using NacreCraft.Codec;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//EntityExactDataComponentsPredicate exact entity components predicate, maps to vanilla net.minecraft.advancements.predicates.entity.EntityExactDataComponentsPredicate
//Vanilla tests the entity's component set; Registry.Entity does not expose components yet, so a non-empty expectation never matches
public sealed record EntityExactDataComponentsPredicate(DataComponentExactPredicate Predicate) : EntitySubPredicate
{
    public static readonly Codec<EntityExactDataComponentsPredicate> Codec =
        DataComponentExactPredicate.CODEC.ComapFlatMap(
            predicate => DataResult<EntityExactDataComponentsPredicate>.Success(
                new EntityExactDataComponentsPredicate(predicate)),
            value => value.Predicate);

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position)
        => Predicate.AlwaysMatches;
}
