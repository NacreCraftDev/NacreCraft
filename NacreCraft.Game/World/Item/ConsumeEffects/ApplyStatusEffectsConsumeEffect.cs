using NacreCraft.Codec;
using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.ConsumeEffects;

//ApplyStatusEffectsConsumeEffect applies a list of mob effects with a probability, maps to vanilla ApplyStatusEffectsConsumeEffect
//Used by suspicious stew and the food component effects; each instance is copied so the consumer does not share the stored instance
public sealed class ApplyStatusEffectsConsumeEffect : ConsumeEffect
{
    //Codec persistence codec, effects list + probability, maps to vanilla CODEC
    public static readonly Codec<ApplyStatusEffectsConsumeEffect> Codec = RecordCodecBuilder.Of2(
        MobEffectInstance.Codec.ListOf().FieldOf("effects")
            .ForGetter((ApplyStatusEffectsConsumeEffect effect) => effect.Effects),
        Codecs.Float.OptionalFieldOf("probability", 1f)
            .ForGetter((ApplyStatusEffectsConsumeEffect effect) => effect.Probability),
        (effects, probability) => new ApplyStatusEffectsConsumeEffect(effects, probability));

    //StreamCodec network codec: list of effect instances then probability, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<RegistryFriendlyByteBuf, ApplyStatusEffectsConsumeEffect> StreamCodec =
        StreamCodecs.Composite<RegistryFriendlyByteBuf, ApplyStatusEffectsConsumeEffect,
            List<MobEffectInstance>, float>(
            ByteBufCodecs.Collection(MobEffectInstance.StreamCodec),
            (ApplyStatusEffectsConsumeEffect effect) =>
                effect.Effects as List<MobEffectInstance> ?? effect.Effects.ToList(),
            ByteBufCodecs.Float,
            (ApplyStatusEffectsConsumeEffect effect) => effect.Probability,
            (effects, probability) => new ApplyStatusEffectsConsumeEffect(effects, probability));

    //Type the registered type object, maps to ConsumeEffect.Type.APPLY_EFFECTS
    public static readonly ConsumeEffectType<ApplyStatusEffectsConsumeEffect> EffectType =
        ConsumeEffectType.Register(Identifier.WithDefaultNamespace("apply_effects"),
            new SimpleConsumeEffectType<ApplyStatusEffectsConsumeEffect>(
                Identifier.WithDefaultNamespace("apply_effects"), Codec, StreamCodec));

    public ApplyStatusEffectsConsumeEffect(IReadOnlyList<MobEffectInstance> effects, float probability)
    {
        Effects = effects;
        Probability = probability;
    }

    public ApplyStatusEffectsConsumeEffect(MobEffectInstance effect, float probability)
        : this(new[] { effect }, probability)
    {
    }

    public ApplyStatusEffectsConsumeEffect(IReadOnlyList<MobEffectInstance> effects) : this(effects, 1f)
    {
    }

    public ApplyStatusEffectsConsumeEffect(MobEffectInstance effect) : this(effect, 1f)
    {
    }

    //Effects the effect instances this consume effect applies
    public IReadOnlyList<MobEffectInstance> Effects { get; }

    //Probability the chance each consumption applies the effects, 0..1
    public float Probability { get; }

    public override ConsumeEffectType Type => EffectType;

    //Apply rolls the probability then adds a copy of every effect, maps to vanilla apply
    //Returns whether any effect was actually applied
    public override bool Apply(ServerLevel level, ItemStack stack, LivingEntity user)
    {
        if (user.GetRandom().NextFloat() >= Probability) return false;
        var anyApplied = false;
        foreach (var effect in Effects)
        {
            if (user.AddEffect(new MobEffectInstance(effect))) anyApplied = true;
        }
        return anyApplied;
    }

    public override string ToString()
        => $"ApplyStatusEffectsConsumeEffect[effects={Effects.Count}, probability={Probability}]";
}
