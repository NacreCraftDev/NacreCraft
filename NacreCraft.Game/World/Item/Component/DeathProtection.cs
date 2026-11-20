using NacreCraft.Codec;
using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Items.ConsumeEffects;
using NacreCraft.Network;

namespace NacreCraft.Game.World.Items.Component;

//DeathProtection effects applied when the holder would die, maps to vanilla net.minecraft.world.item.component.DeathProtection
//The totem constant mirrors the vanilla regeneration/absorption/fire-resistance bundle
public sealed record DeathProtection(IReadOnlyList<ConsumeEffect> DeathEffects)
{
    public static readonly Codec<DeathProtection> Codec = RecordCodecBuilder.Of1(
        ConsumeEffect.Codec.ListOf().OptionalFieldOf("death_effects", Array.Empty<ConsumeEffect>())
            .ForGetter((DeathProtection value) => value.DeathEffects),
        effects => new DeathProtection(effects));

    public static readonly StreamCodec<RegistryFriendlyByteBuf, DeathProtection> StreamCodec =
        ByteBufCodecs.ListOf(ConsumeEffect.StreamCodec)
            .Map(effects => new DeathProtection(effects), value => value.DeathEffects.ToList());

    public static readonly DeathProtection TotemOfUndying = new(new ConsumeEffect[]
    {
        ClearAllStatusEffectsConsumeEffect.Instance,
        new ApplyStatusEffectsConsumeEffect(new MobEffectInstance[]
        {
            new(MobEffects.REGENERATION, 900, 1, false, true),
            new(MobEffects.ABSORPTION, 100, 1, false, true),
            new(MobEffects.FIRE_RESISTANCE, 800, 0, false, true),
        }),
    });
}
