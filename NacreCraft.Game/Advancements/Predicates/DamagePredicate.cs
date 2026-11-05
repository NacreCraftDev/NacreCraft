using NacreCraft.Codec;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Damage;
using EntityPredicate = NacreCraft.Game.Advancements.Predicates.Entity.EntityPredicate;

namespace NacreCraft.Game.Advancements.Predicates;

//DamagePredicate damage event predicate, maps to vanilla net.minecraft.advancements.predicates.DamagePredicate
//Checks the dealt/taken amount, the source entity, the blocked flag and the damage source predicate
public sealed record DamagePredicate(
    MinMaxBounds.Doubles DealtDamage,
    MinMaxBounds.Doubles TakenDamage,
    Optional<EntityPredicate> SourceEntity,
    Optional<bool> Blocked,
    Optional<DamageSourcePredicate> Type)
{
    //Codec persistence codec, field names dealt/taken/source_entity/blocked/type, maps to vanilla CODEC
    public static readonly Codec<DamagePredicate> Codec = RecordCodecBuilder.Of5(
        MinMaxBounds.Doubles.CODEC.OptionalFieldOf("dealt", MinMaxBounds.Doubles.Any)
            .ForGetter((DamagePredicate predicate) => predicate.DealtDamage),
        MinMaxBounds.Doubles.CODEC.OptionalFieldOf("taken", MinMaxBounds.Doubles.Any)
            .ForGetter((DamagePredicate predicate) => predicate.TakenDamage),
        EntityPredicate.Codec.OptionalFieldOf("source_entity")
            .ForGetter((DamagePredicate predicate) => predicate.SourceEntity),
        Codecs.Bool.OptionalFieldOf("blocked").ForGetter((DamagePredicate predicate) => predicate.Blocked),
        DamageSourcePredicate.Codec.OptionalFieldOf("type").ForGetter((DamagePredicate predicate) => predicate.Type),
        (dealt, taken, sourceEntity, blocked, type) =>
            new DamagePredicate(dealt, taken, sourceEntity, blocked, type));

    //Matches every present condition, maps to vanilla matches
    public bool Matches(ServerPlayer player, DamageSource source, float originalDamage, float actualDamage, bool blocked)
    {
        if (!DealtDamage.Matches(originalDamage)) return false;
        if (!TakenDamage.Matches(actualDamage)) return false;
        if (SourceEntity.IsPresent
            && !SourceEntity.Get().Matches(player.Level, player.Position, source.CausingEntity)) return false;
        if (Blocked.IsPresent && Blocked.Get() != blocked) return false;
        return !Type.IsPresent || Type.Get().Matches(player.Level, player.Position, source);
    }

    //Builder wire-up builder, maps to vanilla Builder
    public sealed class Builder
    {
        private MinMaxBounds.Doubles _dealtDamage = MinMaxBounds.Doubles.Any;
        private MinMaxBounds.Doubles _takenDamage = MinMaxBounds.Doubles.Any;
        private Optional<EntityPredicate> _sourceEntity = Optional<EntityPredicate>.Empty();
        private Optional<bool> _blocked = Optional<bool>.Empty();
        private Optional<DamageSourcePredicate> _type = Optional<DamageSourcePredicate>.Empty();

        public static Builder DamageInstance() => new();

        public Builder DealtDamage(MinMaxBounds.Doubles dealtDamage)
        {
            _dealtDamage = dealtDamage;
            return this;
        }

        public Builder TakenDamage(MinMaxBounds.Doubles takenDamage)
        {
            _takenDamage = takenDamage;
            return this;
        }

        public Builder SourceEntity(EntityPredicate sourceEntity)
        {
            _sourceEntity = Optional<EntityPredicate>.Of(sourceEntity);
            return this;
        }

        public Builder Blocked(bool blocked)
        {
            _blocked = Optional<bool>.Of(blocked);
            return this;
        }

        public Builder Type(DamageSourcePredicate type)
        {
            _type = Optional<DamageSourcePredicate>.Of(type);
            return this;
        }

        public DamagePredicate Build() => new(_dealtDamage, _takenDamage, _sourceEntity, _blocked, _type);
    }
}
