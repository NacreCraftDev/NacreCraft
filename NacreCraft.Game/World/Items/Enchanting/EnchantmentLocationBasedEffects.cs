using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Items.Enchanting;

//EnchantmentLocationBasedEffect base for effects that react to where the entity stands, maps to vanilla
//EnchantmentLocationBasedEffect; the entity effect base derives from it, so one table holds both
public abstract record EnchantmentLocationBasedEffect
{
    //OnChangedBlock runs when the effect turns on or off at the current block, maps to vanilla onChangedBlock
    public abstract void OnChangedBlock(in EnchantmentEffectContext context, bool becameActive);

    //OnDeactivated runs when the effect stops applying, most effects have nothing to undo, maps to vanilla onDeactivated
    public virtual void OnDeactivated(in EnchantmentEffectContext context)
    {
    }
}

//AllOfLocationEffects runs a list of location based effects in order, maps to vanilla AllOf.LocationBasedEffects
public sealed record AllOfLocationEffects(IReadOnlyList<EnchantmentLocationBasedEffect> Effects)
    : EnchantmentLocationBasedEffect
{
    public static readonly MapCodec<AllOfLocationEffects> Codec =
        RecordCodecBuilder.Of1<AllOfLocationEffects, IReadOnlyList<EnchantmentLocationBasedEffect>>(
            EnchantmentLocationBasedEffectCodec.Instance.ListOf().FieldOf("effects")
                .ForGetter<AllOfLocationEffects, IReadOnlyList<EnchantmentLocationBasedEffect>>(value => value.Effects),
            effects => new AllOfLocationEffects(effects));

    public override void OnChangedBlock(in EnchantmentEffectContext context, bool becameActive)
    {
        foreach (var effect in Effects) effect.OnChangedBlock(context, becameActive);
    }

    public override void OnDeactivated(in EnchantmentEffectContext context)
    {
        foreach (var effect in Effects) effect.OnDeactivated(context);
    }
}

//EnchantmentAttributeEffect adds an attribute modifier for as long as the effect stays active
public sealed record EnchantmentAttributeEffect(Identifier Id,
    Holder<NacreCraft.Registry.EntityAttribute.Attribute> Attribute, NacreCraft.Registry.LevelBasedValue Amount,
    NacreCraft.Registry.EntityAttribute.AttributeOperation Operation) : EnchantmentLocationBasedEffect
{
    public static readonly MapCodec<EnchantmentAttributeEffect> Codec =
        RecordCodecBuilder.Of4<EnchantmentAttributeEffect, Identifier,
            Holder<NacreCraft.Registry.EntityAttribute.Attribute>, NacreCraft.Registry.LevelBasedValue,
            NacreCraft.Registry.EntityAttribute.AttributeOperation>(
            IdentifierCodec.Instance.FieldOf("id").ForGetter<EnchantmentAttributeEffect, Identifier>(value => value.Id),
            HolderSetCodecs.AttributeRef.FieldOf("attribute")
                .ForGetter<EnchantmentAttributeEffect, Holder<NacreCraft.Registry.EntityAttribute.Attribute>>(
                    value => value.Attribute),
            LevelBasedValueCodec.Instance.FieldOf("amount")
                .ForGetter<EnchantmentAttributeEffect, NacreCraft.Registry.LevelBasedValue>(value => value.Amount),
            NacreCraft.Registry.EntityAttribute.AttributeModifier.OperationCodec.FieldOf("operation")
                .ForGetter<EnchantmentAttributeEffect, NacreCraft.Registry.EntityAttribute.AttributeOperation>(
                    value => value.Operation),
            (id, attribute, amount, operation) => new EnchantmentAttributeEffect(id, attribute, amount, operation));

    public override void OnChangedBlock(in EnchantmentEffectContext context, bool becameActive)
    {
        //Needs the entity attribute map, which arrives with the entity layer
    }
}

//EnchantmentLocationBasedEffectCodec dispatch codec, maps to vanilla EnchantmentLocationBasedEffect.CODEC
//The table stores boxed map codecs, so the value is cast back to the effect codec before decoding
public sealed class EnchantmentLocationBasedEffectCodec : AbstractMapCodec<EnchantmentLocationBasedEffect>
{
    public static readonly EnchantmentLocationBasedEffectCodec Instance = new();

    public override DataResult<EnchantmentLocationBasedEffect> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var typeTag = input.Get("type");
        if (!typeTag.IsPresent)
            return DataResult<EnchantmentLocationBasedEffect>.Error(
                () => "location based effect is missing the type field");
        var typeText = ops.GetStringValue(typeTag.Get());
        if (!typeText.Result().IsPresent)
            return DataResult<EnchantmentLocationBasedEffect>.Error(
                () => "location based effect type is not a string");
        var id = Identifier.TryParse(typeText.GetOrThrow());
        if (id is null)
            return DataResult<EnchantmentLocationBasedEffect>.Error(
                () => $"invalid location based effect type {typeText.GetOrThrow()}");
        var boxed = BuiltInRegistries.ENCHANTMENT_LOCATION_BASED_EFFECT_TYPE.GetValue(id.Value);
        if (boxed is not MapCodec<EnchantmentLocationBasedEffect> codec)
            return DataResult<EnchantmentLocationBasedEffect>.Error(
                () => $"unknown location based effect type {typeText.GetOrThrow()}");
        return codec.Decode(ops, input);
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, EnchantmentLocationBasedEffect value,
        RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding location based effects is not supported yet");
}

//EnchantmentLocationBasedEffects registers every location based effect, maps to vanilla
//EnchantmentLocationBasedEffect.bootstrap
public static class EnchantmentLocationBasedEffects
{
    private static bool _registered;

    public static void Bootstrap()
    {
        if (_registered) return;
        _registered = true;
        Register("all_of", Box(AllOfLocationEffects.Codec));
        Register("apply_mob_effect", Box(ApplyMobEffect.Codec));
        Register("attribute", Box(EnchantmentAttributeEffect.Codec));
        Register("change_item_damage", Box(ChangeItemDamage.Codec));
        Register("damage_entity", Box(DamageEntity.Codec));
        Register("explode", Box(Explode.Codec));
        Register("ignite", Box(Ignite.Codec));
        Register("apply_impulse", Box(ApplyImpulse.Codec));
        Register("apply_exhaustion", Box(ApplyExhaustion.Codec));
        Register("play_sound", Box(PlaySound.Codec));
        Register("replace_block", Box(ReplaceBlock.Codec));
        Register("replace_disk", Box(ReplaceDisk.Codec));
        Register("run_function", Box(RunFunction.Codec));
        Register("set_block_properties", Box(SetBlockProperties.Codec));
        Register("spawn_particles", Box(SpawnParticles.Codec));
        Register("summon_entity", Box(SummonEntity.Codec));
    }

    //Box widens a concrete map codec to the effect codec type the dispatch path casts back to
    private static MapCodec<EnchantmentLocationBasedEffect> Box<T>(MapCodec<T> codec)
        where T : EnchantmentLocationBasedEffect
        => codec.Xmap<T, EnchantmentLocationBasedEffect>(value => value, value => (T)value);

    private static void Register(string id, MapCodec<EnchantmentLocationBasedEffect> codec)
        => Registry<object>.Register(BuiltInRegistries.ENCHANTMENT_LOCATION_BASED_EFFECT_TYPE,
            ResourceKey<object>.Create(Registries.ENCHANTMENT_LOCATION_BASED_EFFECT_TYPE,
                Identifier.WithDefaultNamespace(id)),
            codec);
}
