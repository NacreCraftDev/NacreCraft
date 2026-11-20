using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Items.Component;

//UseCooldown how long an item goes on cooldown after use, maps to vanilla net.minecraft.world.item.component.UseCooldown
public sealed record UseCooldown(float Seconds, Optional<Identifier> CooldownGroup)
{
    public static readonly Codec<UseCooldown> Codec = RecordCodecBuilder.Of2(
        Codecs.Float.FieldOf("seconds").ForGetter((UseCooldown value) => value.Seconds),
        ItemComponentCodecs.IdentifierCodec.OptionalFieldOf("cooldown_group")
            .ForGetter((UseCooldown value) => value.CooldownGroup),
        (seconds, group) => new UseCooldown(seconds, group));

    public static readonly StreamCodec<FriendlyByteBuf, UseCooldown> StreamCodec = StreamCodecs.Composite(
        ByteBufCodecs.Float, value => value.Seconds,
        ByteBufCodecs.Optional(ItemComponentCodecs.IdentifierStreamCodec), value => value.CooldownGroup,
        (seconds, group) => new UseCooldown(seconds, group));

    public UseCooldown(float seconds) : this(seconds, Optional<Identifier>.Empty())
    {
    }
}

//UseEffects how an item affects the user while it is being used, maps to vanilla net.minecraft.world.item.component.UseEffects
public sealed record UseEffects(bool CanSprint, bool InteractVibrations, float SpeedMultiplier)
{
    public static readonly UseEffects Default = new(false, true, 0.2f);

    public static readonly Codec<UseEffects> Codec = RecordCodecBuilder.Of3(
        Codecs.Bool.OptionalFieldOf("can_sprint", Default.CanSprint).ForGetter((UseEffects value) => value.CanSprint),
        Codecs.Bool.OptionalFieldOf("interact_vibrations", Default.InteractVibrations)
            .ForGetter((UseEffects value) => value.InteractVibrations),
        Codecs.Float.OptionalFieldOf("speed_multiplier", Default.SpeedMultiplier)
            .ForGetter((UseEffects value) => value.SpeedMultiplier),
        (canSprint, vibrations, speed) => new UseEffects(canSprint, vibrations, speed));

    public static readonly StreamCodec<FriendlyByteBuf, UseEffects> StreamCodec = StreamCodecs.Composite(
        ByteBufCodecs.Bool, value => value.CanSprint,
        ByteBufCodecs.Bool, value => value.InteractVibrations,
        ByteBufCodecs.Float, value => value.SpeedMultiplier,
        (canSprint, vibrations, speed) => new UseEffects(canSprint, vibrations, speed));
}

//OminousBottleAmplifier the amplifier of the bad omen granted by an ominous bottle, maps to vanilla net.minecraft.world.item.component.OminousBottleAmplifier
public sealed record OminousBottleAmplifier(int Value)
{
    public const int EffectDuration = 120000;
    public const int MinAmplifier = 0;
    public const int MaxAmplifier = 4;

    public static readonly Codec<OminousBottleAmplifier> Codec =
        ExtraCodecs.IntRange(MinAmplifier, MaxAmplifier).Xmap(value => new OminousBottleAmplifier(value), value => value.Value);

    public static readonly StreamCodec<FriendlyByteBuf, OminousBottleAmplifier> StreamCodec =
        ByteBufCodecs.VarInt.Map(value => new OminousBottleAmplifier(value), value => value.Value);
}
