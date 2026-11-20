using NacreCraft.Codec;
using NacreCraft.Registry.Codec;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Items.Component;

//KineticWeapon the mounted/mace-style kinetic attack data, maps to vanilla net.minecraft.world.item.component.KineticWeapon
//Conditions select when the attack dismounts, knocks back or deals damage; NC keeps the data and codecs, the combat resolution
//stays in the future attack pipeline
public sealed record KineticWeapon(
    int ContactCooldownTicks,
    int DelayTicks,
    Optional<KineticWeapon.Condition> DismountConditions,
    Optional<KineticWeapon.Condition> KnockbackConditions,
    Optional<KineticWeapon.Condition> DamageConditions,
    float ForwardMovement,
    float DamageMultiplier,
    Optional<SoundEvent> Sound,
    Optional<SoundEvent> HitSound)
{
    public const int HitFeedbackTicks = 10;

    public static readonly Codec<KineticWeapon> Codec = RecordCodecBuilder.Of9(
        ExtraCodecs.NonNegativeInt.OptionalFieldOf("contact_cooldown_ticks", 10)
            .ForGetter((KineticWeapon value) => value.ContactCooldownTicks),
        ExtraCodecs.NonNegativeInt.OptionalFieldOf("delay_ticks", 0)
            .ForGetter((KineticWeapon value) => value.DelayTicks),
        Condition.Codec.OptionalFieldOf("dismount_conditions")
            .ForGetter((KineticWeapon value) => value.DismountConditions),
        Condition.Codec.OptionalFieldOf("knockback_conditions")
            .ForGetter((KineticWeapon value) => value.KnockbackConditions),
        Condition.Codec.OptionalFieldOf("damage_conditions")
            .ForGetter((KineticWeapon value) => value.DamageConditions),
        Codecs.Float.OptionalFieldOf("forward_movement", 0f)
            .ForGetter((KineticWeapon value) => value.ForwardMovement),
        Codecs.Float.OptionalFieldOf("damage_multiplier", 1f)
            .ForGetter((KineticWeapon value) => value.DamageMultiplier),
        SoundEventCodec.Instance.OptionalFieldOf("sound").ForGetter((KineticWeapon value) => value.Sound),
        SoundEventCodec.Instance.OptionalFieldOf("hit_sound").ForGetter((KineticWeapon value) => value.HitSound),
        (cooldown, delay, dismount, knockback, damage, forward, multiplier, sound, hitSound)
            => new KineticWeapon(cooldown, delay, dismount, knockback, damage, forward, multiplier, sound, hitSound));

    //Condition the threshold a kinetic effect needs, maps to vanilla KineticWeapon.Condition
    public sealed record Condition(int MaxDurationTicks, float MinSpeed, float MinRelativeSpeed)
    {
        public static readonly Codec<Condition> Codec = RecordCodecBuilder.Of3(
            ExtraCodecs.NonNegativeInt.FieldOf("max_duration_ticks")
                .ForGetter((Condition value) => value.MaxDurationTicks),
            Codecs.Float.OptionalFieldOf("min_speed", 0f).ForGetter((Condition value) => value.MinSpeed),
            Codecs.Float.OptionalFieldOf("min_relative_speed", 0f)
                .ForGetter((Condition value) => value.MinRelativeSpeed),
            (duration, speed, relativeSpeed) => new Condition(duration, speed, relativeSpeed));
    }
}

//PiercingWeapon the spear-style piercing attack data, maps to vanilla net.minecraft.world.item.component.PiercingWeapon
public sealed record PiercingWeapon(
    bool DealsKnockback,
    bool Dismounts,
    Optional<SoundEvent> Sound,
    Optional<SoundEvent> HitSound)
{
    public static readonly Codec<PiercingWeapon> Codec = RecordCodecBuilder.Of4(
        Codecs.Bool.OptionalFieldOf("deals_knockback", true)
            .ForGetter((PiercingWeapon value) => value.DealsKnockback),
        Codecs.Bool.OptionalFieldOf("dismounts", false).ForGetter((PiercingWeapon value) => value.Dismounts),
        SoundEventCodec.Instance.OptionalFieldOf("sound").ForGetter((PiercingWeapon value) => value.Sound),
        SoundEventCodec.Instance.OptionalFieldOf("hit_sound").ForGetter((PiercingWeapon value) => value.HitSound),
        (knockback, dismounts, sound, hitSound) => new PiercingWeapon(knockback, dismounts, sound, hitSound));
}
