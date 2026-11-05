using NacreCraft.Codec;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//FishingHookPredicate fishing hook predicate, maps to vanilla net.minecraft.advancements.predicates.entity.FishingHookPredicate
//An absent requirement matches anything; the fishing hook entity is not ported, so a stated requirement never matches
public sealed record FishingHookPredicate(Optional<bool> InOpenWater) : EntitySubPredicate
{
    public static readonly FishingHookPredicate Any = new(Optional<bool>.Empty());

    public static readonly Codec<FishingHookPredicate> Codec =
        RecordCodecBuilder.Of1<FishingHookPredicate, Optional<bool>>(
            Codecs.Bool.OptionalFieldOf("in_open_water")
                .ForGetter<FishingHookPredicate, Optional<bool>>(predicate => predicate.InOpenWater),
            inOpenWater => new FishingHookPredicate(inOpenWater));

    public static FishingHookPredicate InOpenWaterOf(bool requirement) => new(Optional<bool>.Of(requirement));

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position)
        => !InOpenWater.IsPresent;
}
