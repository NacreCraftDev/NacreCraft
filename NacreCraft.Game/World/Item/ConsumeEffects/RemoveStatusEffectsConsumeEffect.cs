using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.ConsumeEffects;

//RemoveStatusEffectsConsumeEffect removes a set of mob effects from the consumer, maps to vanilla RemoveStatusEffectsConsumeEffect
//The set may be a tag or a direct list; the persistence codec keeps the holder-set form, the stream codec expands it to a direct list
public sealed class RemoveStatusEffectsConsumeEffect : ConsumeEffect
{
    //Codec persistence codec, an effects holder set under the effects field, maps to vanilla CODEC
    public static readonly Codec<RemoveStatusEffectsConsumeEffect> Codec = RecordCodecBuilder.Of1(
        HolderSetCodecs.MobEffectSet.FieldOf("effects")
            .ForGetter((RemoveStatusEffectsConsumeEffect effect) => effect.Effects),
        effects => new RemoveStatusEffectsConsumeEffect(effects));

    //StreamCodec network codec, effect holder set, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<RegistryFriendlyByteBuf, RemoveStatusEffectsConsumeEffect> StreamCodec =
        new RemoveStatusEffectsStreamCodec();

    public static readonly ConsumeEffectType<RemoveStatusEffectsConsumeEffect> EffectType =
        ConsumeEffectType.Register(Identifier.WithDefaultNamespace("remove_effects"),
            new SimpleConsumeEffectType<RemoveStatusEffectsConsumeEffect>(
                Identifier.WithDefaultNamespace("remove_effects"), Codec, StreamCodec));

    public RemoveStatusEffectsConsumeEffect(HolderSet<NacreCraft.Registry.MobEffect> effects) => Effects = effects;

    //Effects the effect set this consume effect removes
    public HolderSet<NacreCraft.Registry.MobEffect> Effects { get; }

    public override ConsumeEffectType Type => EffectType;

    //Apply removes every listed effect and reports whether any was active, maps to vanilla apply
    public override bool Apply(ServerLevel level, ItemStack stack, LivingEntity user)
    {
        var removedAny = false;
        foreach (var effect in Effects)
        {
            if (user.RemoveEffect(effect)) removedAny = true;
        }
        return removedAny;
    }

    public override string ToString() => $"RemoveStatusEffectsConsumeEffect[effects={Effects.Size}]";
}

//RemoveStatusEffectsStreamCodec writes the holder set as a VarInt count followed by the registry ids
internal sealed class RemoveStatusEffectsStreamCodec
    : StreamCodec<RegistryFriendlyByteBuf, RemoveStatusEffectsConsumeEffect>
{
    private readonly StreamCodec<RegistryFriendlyByteBuf, Holder<NacreCraft.Registry.MobEffect>> _holderCodec =
        ByteBufCodecs.Holder(Registries.MOB_EFFECT);

    public RemoveStatusEffectsConsumeEffect Decode(RegistryFriendlyByteBuf buf)
    {
        var size = buf.ReadVarInt();
        var effects = new List<Holder<NacreCraft.Registry.MobEffect>>(
            Math.Min(size, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < size; i++) effects.Add(_holderCodec.Decode(buf));
        return new RemoveStatusEffectsConsumeEffect(new DirectHolderSet<NacreCraft.Registry.MobEffect>(effects));
    }

    public void Encode(RegistryFriendlyByteBuf buf, RemoveStatusEffectsConsumeEffect value)
    {
        buf.WriteVarInt(value.Effects.Size);
        for (var i = 0; i < value.Effects.Size; i++)
            _holderCodec.Encode(buf, value.Effects.Get(i));
    }
}
