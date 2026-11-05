using NacreCraft.Codec;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//CubeMobPredicate cube mob size predicate, maps to vanilla net.minecraft.advancements.predicates.entity.CubeMobPredicate
//Vanilla checks the slime/magma cube size; the cube mob hierarchy is not ported
public sealed record CubeMobPredicate(MinMaxBounds.Ints Size) : EntitySubPredicate
{
    public static readonly Codec<CubeMobPredicate> Codec = RecordCodecBuilder.Of1<CubeMobPredicate, MinMaxBounds.Ints>(
        MinMaxBounds.Ints.CODEC.OptionalFieldOf("size", MinMaxBounds.Ints.Any)
            .ForGetter<CubeMobPredicate, MinMaxBounds.Ints>(predicate => predicate.Size),
        size => new CubeMobPredicate(size));

    public static CubeMobPredicate Sized(MinMaxBounds.Ints size) => new(size);

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position) => false;
}
