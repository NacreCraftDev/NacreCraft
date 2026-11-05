using NacreCraft.Codec;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//TargetedEntityPredicate targeted entity predicate, maps to vanilla net.minecraft.advancements.predicates.entity.TargetedEntityPredicate
//Vanilla asks a mob for its current target; mob targeting is not ported, so no entity has a target
public sealed record TargetedEntityPredicate(EntityPredicate Target) : EntitySubPredicate
{
    public static readonly Codec<TargetedEntityPredicate> Codec = EntityPredicate.Codec.ComapFlatMap(
        predicate => DataResult<TargetedEntityPredicate>.Success(new TargetedEntityPredicate(predicate)),
        predicate => predicate.Target);

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position) => false;
}
