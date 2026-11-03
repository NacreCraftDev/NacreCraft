using NacreCraft.Codec;
using NacreCraft.Game.World.Damage;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using EntityPredicate = NacreCraft.Game.Advancements.Predicates.Entity.EntityPredicate;

namespace NacreCraft.Game.Advancements.Predicates;

//DamageSourcePredicate damage source predicate, checks the damage type tags and the entities behind the damage
//maps to vanilla net.minecraft.advancements.predicates.DamageSourcePredicate
public sealed record DamageSourcePredicate(
    IReadOnlyList<TagPredicate<NacreCraft.Registry.DamageType>> Tags,
    Optional<EntityPredicate> DirectEntity,
    Optional<EntityPredicate> SourceEntity,
    Optional<bool> IsDirect)
{
    private static readonly IReadOnlyList<TagPredicate<NacreCraft.Registry.DamageType>> NoTags =
        Array.Empty<TagPredicate<NacreCraft.Registry.DamageType>>();

    //Codec persistence codec, field names tags direct_entity source_entity is_direct, maps to vanilla CODEC
    public static readonly Codec<DamageSourcePredicate> Codec = RecordCodecBuilder.Of4(
        TagPredicate<NacreCraft.Registry.DamageType>.Codec(Registries.DAMAGE_TYPE).ListOf()
            .OptionalFieldOf("tags", NoTags).ForGetter((DamageSourcePredicate predicate) => predicate.Tags),
        EntityPredicate.Codec.OptionalFieldOf("direct_entity")
            .ForGetter((DamageSourcePredicate predicate) => predicate.DirectEntity),
        EntityPredicate.Codec.OptionalFieldOf("source_entity")
            .ForGetter((DamageSourcePredicate predicate) => predicate.SourceEntity),
        Codecs.Bool.OptionalFieldOf("is_direct").ForGetter((DamageSourcePredicate predicate) => predicate.IsDirect),
        (tags, directEntity, sourceEntity, isDirect) =>
            new DamageSourcePredicate(tags, directEntity, sourceEntity, isDirect));

    //Matches every tag against the damage type then checks the optional entity predicates, maps to vanilla matches
    public bool Matches(ServerLevel level, Vec3 position, DamageSource source)
    {
        foreach (var tag in Tags)
            if (!tag.Matches(source.TypeHolder))
                return false;
        if (DirectEntity.IsPresent && !DirectEntity.Get().Matches(level, position, source.DirectEntity)) return false;
        if (SourceEntity.IsPresent && !SourceEntity.Get().Matches(level, position, source.CausingEntity)) return false;
        return !IsDirect.IsPresent || IsDirect.Get() == source.IsDirect();
    }
}
