using NacreCraft.Codec;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//PassengerPredicate passenger predicate, maps to vanilla net.minecraft.advancements.predicates.entity.PassengerPredicate
//Vanilla walks the entity's passengers; the rider/passenger hierarchy is not ported, so nothing is a passenger here
public sealed record PassengerPredicate(EntityPredicate Passenger) : EntitySubPredicate
{
    public static readonly Codec<PassengerPredicate> Codec = EntityPredicate.Codec.ComapFlatMap(
        predicate => DataResult<PassengerPredicate>.Success(new PassengerPredicate(predicate)),
        predicate => predicate.Passenger);

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position) => false;
}
