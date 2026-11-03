using NacreCraft.Codec;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//EntityVehiclePredicate the entity must be riding something that satisfies the nested predicate, maps to vanilla
//net.minecraft.advancements.predicates.entity.EntityVehiclePredicate
//The entity layer has no riding relationship yet, so nothing is ever on a vehicle and an absent predicate always matches
public sealed record EntityVehiclePredicate(Optional<EntityPredicate> Vehicle) : EntitySubPredicate
{
    //Codec persistence codec, field name vehicle, maps to vanilla CODEC
    public static readonly Codec<EntityVehiclePredicate> Codec = RecordCodecBuilder.Of1(
        EntityPredicate.Codec.OptionalFieldOf("vehicle")
            .ForGetter((EntityVehiclePredicate predicate) => predicate.Vehicle),
        vehicle => new EntityVehiclePredicate(vehicle));

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position)
        => !Vehicle.IsPresent;
}
