using NacreCraft.Codec;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//EntityPartialComponentsPredicate partial entity components predicate, maps to vanilla net.minecraft.advancements.predicates.entity.EntityPartialComponentsPredicate
//Vanilla tests each component predicate against the entity; Registry.Entity does not expose components yet, so any predicate fails
public sealed record EntityPartialComponentsPredicate(
    IReadOnlyDictionary<DataComponentPredicate.Type, DataComponentPredicate> Predicates) : EntitySubPredicate
{
    public static readonly Codec<EntityPartialComponentsPredicate> Codec =
        DataComponentPredicate.CODEC.ComapFlatMap(
            predicates => DataResult<EntityPartialComponentsPredicate>.Success(
                new EntityPartialComponentsPredicate(predicates)),
            value => value.Predicates.ToDictionary(entry => entry.Key, entry => entry.Value));

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position)
        => Predicates.Count == 0;
}
