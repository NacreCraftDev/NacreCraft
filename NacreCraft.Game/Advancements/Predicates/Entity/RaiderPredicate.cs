using NacreCraft.Codec;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//RaiderPredicate raider predicate, maps to vanilla net.minecraft.advancements.predicates.entity.RaiderPredicate
//Vanilla reads the raid state and captain flag of a Raider; the raid subsystem is not ported
public sealed record RaiderPredicate(bool HasRaid, bool IsCaptain) : EntitySubPredicate
{
    public static readonly RaiderPredicate CaptainWithoutRaid = new(false, true);

    public static readonly Codec<RaiderPredicate> Codec = RecordCodecBuilder.Of2(
        Codecs.Bool.OptionalFieldOf("has_raid", false)
            .ForGetter((RaiderPredicate predicate) => predicate.HasRaid),
        Codecs.Bool.OptionalFieldOf("is_captain", false)
            .ForGetter((RaiderPredicate predicate) => predicate.IsCaptain),
        (hasRaid, isCaptain) => new RaiderPredicate(hasRaid, isCaptain));

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position) => false;
}
