using NacreCraft.Codec;
using NacreCraft.Game.World.Damage;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Items.Component;

//BlocksAttacks the blocking behavior of a shield-like item, maps to vanilla net.minecraft.world.item.component.BlocksAttacks
//Damage reduction, item damage and bypass checks are ported; sound/cooldown side effects stay with the combat layer
public sealed record BlocksAttacks(
    float BlockDelaySeconds,
    float DisableCooldownScale,
    IReadOnlyList<BlocksAttacks.DamageReduction> DamageReductions,
    BlocksAttacks.ItemDamageFunction ItemDamage,
    Optional<HolderSet<DamageType>> BypassedBy,
    Optional<SoundEvent> BlockSound,
    Optional<SoundEvent> DisableSound)
{
    public static readonly Codec<BlocksAttacks> Codec = RecordCodecBuilder.Of7(
        Codecs.Float.OptionalFieldOf("block_delay_seconds", 0f)
            .ForGetter((BlocksAttacks value) => value.BlockDelaySeconds),
        Codecs.Float.OptionalFieldOf("disable_cooldown_scale", 1f)
            .ForGetter((BlocksAttacks value) => value.DisableCooldownScale),
        DamageReduction.Codec.ListOf()
            .OptionalFieldOf("damage_reductions", new[] { new DamageReduction(90f, Optional<HolderSet<DamageType>>.Empty(), 0f, 1f) })
            .ForGetter((BlocksAttacks value) => value.DamageReductions),
        ItemDamageFunction.Codec.OptionalFieldOf("item_damage", ItemDamageFunction.Default)
            .ForGetter((BlocksAttacks value) => value.ItemDamage),
        HolderSetCodecs.DamageTypeSet.OptionalFieldOf("bypassed_by")
            .ForGetter((BlocksAttacks value) => value.BypassedBy),
        SoundEventCodec.Instance.OptionalFieldOf("block_sound").ForGetter((BlocksAttacks value) => value.BlockSound),
        SoundEventCodec.Instance.OptionalFieldOf("disabled_sound").ForGetter((BlocksAttacks value) => value.DisableSound),
        (delay, scale, reductions, itemDamage, bypassed, blockSound, disableSound)
            => new BlocksAttacks(delay, scale, reductions, itemDamage, bypassed, blockSound, disableSound));

    public int BlockDelayTicks => (int)(BlockDelaySeconds * 20f);

    //ResolveBlockedDamage the damage left after the matching reduction rule, maps to vanilla resolveBlockedDamage
    public float ResolveBlockedDamage(DamageSource source, float dealtDamage, double angle)
    {
        foreach (var reduction in DamageReductions)
        {
            if (!reduction.Matches(source)) continue;
            return reduction.Resolve(dealtDamage, angle);
        }
        return 0f;
    }

    //DamageReduction one blocking rule, maps to vanilla BlocksAttacks.DamageReduction
    public sealed record DamageReduction(
        float HorizontalBlockingAngle,
        Optional<HolderSet<DamageType>> Type,
        float Base,
        float Factor)
    {
        public static readonly Codec<DamageReduction> Codec = RecordCodecBuilder.Of4(
            Codecs.Float.OptionalFieldOf("horizontal_blocking_angle", 90f)
                .ForGetter((DamageReduction value) => value.HorizontalBlockingAngle),
            HolderSetCodecs.DamageTypeSet.OptionalFieldOf("type").ForGetter((DamageReduction value) => value.Type),
            Codecs.Float.FieldOf("base").ForGetter((DamageReduction value) => value.Base),
            Codecs.Float.FieldOf("factor").ForGetter((DamageReduction value) => value.Factor),
            (angle, type, @base, factor) => new DamageReduction(angle, type, @base, factor));

        public bool Matches(DamageSource source) => !Type.IsPresent || Type.Get().Contains(source.TypeHolder);

        //Resolve applies base + factor * dealtDamage when the hit is inside the blocking angle, maps to vanilla resolve
        public float Resolve(float dealtDamage, double angle)
            => Math.Abs(angle) <= HorizontalBlockingAngle ? Base + Factor * dealtDamage : 0f;
    }

    //ItemDamageFunction how much durability a blocked hit costs, maps to vanilla BlocksAttacks.ItemDamageFunction
    public sealed record ItemDamageFunction(float Threshold, float Base, float Factor)
    {
        public static readonly ItemDamageFunction Default = new(1f, 0f, 1f);

        public static readonly Codec<ItemDamageFunction> Codec = RecordCodecBuilder.Of3(
            Codecs.Float.FieldOf("threshold").ForGetter((ItemDamageFunction value) => value.Threshold),
            Codecs.Float.FieldOf("base").ForGetter((ItemDamageFunction value) => value.Base),
            Codecs.Float.FieldOf("factor").ForGetter((ItemDamageFunction value) => value.Factor),
            (threshold, @base, factor) => new ItemDamageFunction(threshold, @base, factor));

        public int Apply(float dealtDamage)
            => dealtDamage < Threshold ? 0 : (int)(Base + Factor * dealtDamage);
    }
}
