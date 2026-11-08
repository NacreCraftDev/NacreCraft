using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.ConsumeEffects;

//ClearAllStatusEffectsConsumeEffect removes every active effect, maps to vanilla ClearAllStatusEffectsConsumeEffect
//A unit effect: neither codec writes any field
public sealed class ClearAllStatusEffectsConsumeEffect : ConsumeEffect
{
    //Instance the shared singleton, maps to vanilla INSTANCE
    public static readonly ClearAllStatusEffectsConsumeEffect Instance = new();

    //Codec unit map codec; UnitMapCodec is an AbstractMapCodec and therefore also a Codec, maps to vanilla CODEC
    public static readonly Codec<ClearAllStatusEffectsConsumeEffect> Codec =
        (Codec<ClearAllStatusEffectsConsumeEffect>)Codecs.Unit(Instance);

    //StreamCodec unit codec, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<RegistryFriendlyByteBuf, ClearAllStatusEffectsConsumeEffect> StreamCodec =
        StreamCodecExtensions.Unit<RegistryFriendlyByteBuf, ClearAllStatusEffectsConsumeEffect>(Instance);

    public static readonly ConsumeEffectType<ClearAllStatusEffectsConsumeEffect> EffectType =
        ConsumeEffectType.Register(Identifier.WithDefaultNamespace("clear_all_effects"),
            new SimpleConsumeEffectType<ClearAllStatusEffectsConsumeEffect>(
                Identifier.WithDefaultNamespace("clear_all_effects"), Codec, StreamCodec));

    private ClearAllStatusEffectsConsumeEffect()
    {
    }

    public override ConsumeEffectType Type => EffectType;

    //Apply clears every effect and reports whether anything was removed, maps to vanilla apply
    public override bool Apply(ServerLevel level, ItemStack stack, LivingEntity user) => user.RemoveAllEffects();

    public override string ToString() => "ClearAllStatusEffectsConsumeEffect";
}
