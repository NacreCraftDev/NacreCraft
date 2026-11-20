using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Items.Component;

//AttackRange the reach and hitbox margin of an attack, maps to vanilla net.minecraft.world.item.component.AttackRange
//NC has no clip-based closest-hit query yet, so only the range math is ported
public sealed record AttackRange(
    float MinReach,
    float MaxReach,
    float MinCreativeReach,
    float MaxCreativeReach,
    float HitboxMargin,
    float MobFactor)
{
    public static readonly Codec<AttackRange> Codec = RecordCodecBuilder.Of6(
        Codecs.Float.OptionalFieldOf("min_reach", 0f).ForGetter((AttackRange value) => value.MinReach),
        Codecs.Float.OptionalFieldOf("max_reach", 3f).ForGetter((AttackRange value) => value.MaxReach),
        Codecs.Float.OptionalFieldOf("min_creative_reach", 0f).ForGetter((AttackRange value) => value.MinCreativeReach),
        Codecs.Float.OptionalFieldOf("max_creative_reach", 5f).ForGetter((AttackRange value) => value.MaxCreativeReach),
        Codecs.Float.OptionalFieldOf("hitbox_margin", 0.3f).ForGetter((AttackRange value) => value.HitboxMargin),
        Codecs.Float.OptionalFieldOf("mob_factor", 1f).ForGetter((AttackRange value) => value.MobFactor),
        (minReach, maxReach, minCreative, maxCreative, margin, mobFactor)
            => new AttackRange(minReach, maxReach, minCreative, maxCreative, margin, mobFactor));

    public static readonly StreamCodec<FriendlyByteBuf, AttackRange> StreamCodec = StreamCodecs.Composite(
        ByteBufCodecs.Float, value => value.MinReach,
        ByteBufCodecs.Float, value => value.MaxReach,
        ByteBufCodecs.Float, value => value.MinCreativeReach,
        ByteBufCodecs.Float, value => value.MaxCreativeReach,
        ByteBufCodecs.Float, value => value.HitboxMargin,
        ByteBufCodecs.Float, value => value.MobFactor,
        (minReach, maxReach, minCreative, maxCreative, margin, mobFactor)
            => new AttackRange(minReach, maxReach, minCreative, maxCreative, margin, mobFactor));

    //DefaultFor the default range of a living entity, maps to vanilla defaultFor; the interaction-range attribute is not ported
    public static AttackRange DefaultFor(LivingEntity entity)
        => new(0f, 3f, 0f, 3f, 0.3f, 1f);

    //EffectiveMinRange the minimum reach for the attacker, maps to vanilla effectiveMinRange
    public float EffectiveMinRange(NacreCraft.Registry.Entity entity)
        => entity is Player { GameMode: var mode } && mode == NacreCraft.Game.World.Level.GameType.Creative.Id
            ? MinCreativeReach
            : entity is Player ? MinReach : MinReach * MobFactor;

    //EffectiveMaxRange the maximum reach for the attacker, maps to vanilla effectiveMaxRange
    public float EffectiveMaxRange(NacreCraft.Registry.Entity entity)
        => entity is Player { GameMode: var mode } && mode == NacreCraft.Game.World.Level.GameType.Creative.Id
            ? MaxCreativeReach
            : entity is Player ? MaxReach : MaxReach * MobFactor;

    //IsInRange whether the location is inside the reach band around the attacker's eyes
    public bool IsInRange(LivingEntity attacker, Vec3 location)
    {
        var distance = Math.Sqrt(attacker.GetEyePosition().DistanceToSqr(location));
        var minReach = EffectiveMinRange(attacker) - HitboxMargin;
        var maxReach = EffectiveMaxRange(attacker) + HitboxMargin;
        return distance >= minReach && distance <= maxReach;
    }
}

//WeaponToolDamage placeholder to keep the file focused on AttackRange
