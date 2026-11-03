using NacreCraft.Codec;
using NacreCraft.Game.World.Loot;
//The registry layer also declares an empty LootItemCondition stub, the loot one is the real type here
using LootCondition = NacreCraft.Game.World.Loot.LootItemCondition;

namespace NacreCraft.Game.World.Items.Enchanting;

//EnchantmentTarget which side of an interaction an effect targets, maps to vanilla EnchantmentTarget
public enum EnchantmentTarget
{
    Attacker,
    DamagingEntity,
    Victim,
}

//EnchantmentTargets name codecs for the target enum, maps to vanilla EnchantmentTarget.CODEC and NON_DAMAGE_CODEC
public static class EnchantmentTargets
{
    //Codec accepts all three targets
    public static readonly Codec<EnchantmentTarget> Codec = NameCodec(null);

    //NonDamageCodec rejects damaging_entity, only the equipment drop path uses it
    public static readonly Codec<EnchantmentTarget> NonDamageCodec = NameCodec(EnchantmentTarget.DamagingEntity);

    //GetSerializedName serialized name of a target
    public static string GetSerializedName(EnchantmentTarget target) => target switch
    {
        EnchantmentTarget.Attacker => "attacker",
        EnchantmentTarget.DamagingEntity => "damaging_entity",
        _ => "victim",
    };

    private static Codec<EnchantmentTarget> NameCodec(EnchantmentTarget? excluded)
        => Codecs.String.ComapFlatMap(
            name => TryParse(name) is { } target && target != excluded
                ? DataResult<EnchantmentTarget>.Success(target)
                : DataResult<EnchantmentTarget>.Error(() => $"Unknown enchantment target {name}"),
            GetSerializedName);

    private static EnchantmentTarget? TryParse(string name) => name switch
    {
        "attacker" => EnchantmentTarget.Attacker,
        "damaging_entity" => EnchantmentTarget.DamagingEntity,
        "victim" => EnchantmentTarget.Victim,
        _ => null,
    };
}

//ConditionalEffect an effect plus the loot condition gating it, maps to vanilla ConditionalEffect
public sealed record ConditionalEffect<T>(T Effect, Optional<LootCondition> Requirements)
{
    //Create builds the codec for one effect type, maps to vanilla ConditionalEffect.codec
    public static Codec<ConditionalEffect<T>> Create(Codec<T> effectCodec)
        => RecordCodecBuilder.Of2<ConditionalEffect<T>, T, Optional<LootCondition>>(
            effectCodec.FieldOf("effect").ForGetter<ConditionalEffect<T>, T>(value => value.Effect),
            LootItemConditions.TYPED_CODEC.OptionalFieldOf("requirements")
                .ForGetter<ConditionalEffect<T>, Optional<LootCondition>>(value => value.Requirements),
            (effect, requirements) => new ConditionalEffect<T>(effect, requirements));

    //Matches whether the requirements pass, absent requirements always match
    public bool Matches(LootContext context)
        => !Requirements.IsPresent || Requirements.Get().Test(context);
}

//TargetedConditionalEffect a conditional effect that names both sides it acts on, maps to vanilla TargetedConditionalEffect
public sealed record TargetedConditionalEffect<T>(EnchantmentTarget Enchanted, EnchantmentTarget Affected, T Effect,
    Optional<LootCondition> Requirements)
{
    //Create builds the codec with both target fields, maps to vanilla TargetedConditionalEffect.codec
    public static Codec<TargetedConditionalEffect<T>> Create(Codec<T> effectCodec)
        => RecordCodecBuilder.Of4<TargetedConditionalEffect<T>, EnchantmentTarget, EnchantmentTarget, T,
            Optional<LootCondition>>(
            EnchantmentTargets.Codec.FieldOf("enchanted")
                .ForGetter<TargetedConditionalEffect<T>, EnchantmentTarget>(value => value.Enchanted),
            EnchantmentTargets.Codec.FieldOf("affected")
                .ForGetter<TargetedConditionalEffect<T>, EnchantmentTarget>(value => value.Affected),
            effectCodec.FieldOf("effect").ForGetter<TargetedConditionalEffect<T>, T>(value => value.Effect),
            LootItemConditions.TYPED_CODEC.OptionalFieldOf("requirements")
                .ForGetter<TargetedConditionalEffect<T>, Optional<LootCondition>>(value => value.Requirements),
            (enchanted, affected, effect, requirements) =>
                new TargetedConditionalEffect<T>(enchanted, affected, effect, requirements));

    //EquipmentDropsCodec writes only the enchanted side and always drops from the victim, maps to vanilla equipmentDropsCodec
    public static Codec<TargetedConditionalEffect<T>> EquipmentDropsCodec(Codec<T> effectCodec)
        => RecordCodecBuilder.Of3<TargetedConditionalEffect<T>, EnchantmentTarget, T, Optional<LootCondition>>(
            EnchantmentTargets.NonDamageCodec.FieldOf("enchanted")
                .ForGetter<TargetedConditionalEffect<T>, EnchantmentTarget>(value => value.Enchanted),
            effectCodec.FieldOf("effect").ForGetter<TargetedConditionalEffect<T>, T>(value => value.Effect),
            LootItemConditions.TYPED_CODEC.OptionalFieldOf("requirements")
                .ForGetter<TargetedConditionalEffect<T>, Optional<LootCondition>>(value => value.Requirements),
            (enchanted, effect, requirements) =>
                new TargetedConditionalEffect<T>(enchanted, EnchantmentTarget.Victim, effect, requirements));

    //Matches whether the requirements pass, absent requirements always match
    public bool Matches(LootContext context)
        => !Requirements.IsPresent || Requirements.Get().Test(context);
}

//DamageImmunity marks an enchantment as immune to a kind of damage, maps to vanilla DamageImmunity
//The record carries no data, so the codec reads an empty object like vanilla unitCodec
public sealed record DamageImmunity
{
    public static readonly DamageImmunity Instance = new();

    public static readonly Codec<DamageImmunity> Codec = Codecs.AsCodec(Codecs.Unit(Instance));

    private DamageImmunity()
    {
    }
}
