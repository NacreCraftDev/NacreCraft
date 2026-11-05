using NacreCraft.Codec;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//SheepPredicate sheep predicate, maps to vanilla net.minecraft.advancements.predicates.entity.SheepPredicate
//Vanilla checks the sheared flag; the sheep entity is not ported
public sealed record SheepPredicate(Optional<bool> Sheared) : EntitySubPredicate
{
    public static readonly Codec<SheepPredicate> Codec = RecordCodecBuilder.Of1<SheepPredicate, Optional<bool>>(
        Codecs.Bool.OptionalFieldOf("sheared")
            .ForGetter<SheepPredicate, Optional<bool>>(predicate => predicate.Sheared),
        sheared => new SheepPredicate(sheared));

    public static SheepPredicate HasWool() => new(Optional<bool>.Of(false));

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position) => false;
}
