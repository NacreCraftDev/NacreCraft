using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Items.Component;

//SwingAnimation the swing animation and its duration, maps to vanilla net.minecraft.world.item.component.SwingAnimation
public sealed record SwingAnimation(SwingAnimationType Type, int Duration)
{
    public static readonly SwingAnimation Default = new(SwingAnimationType.Whack, 6);

    public static readonly Codec<SwingAnimation> Codec = RecordCodecBuilder.Of2(
        SwingAnimationTypeExtensions.Codec.OptionalFieldOf("type", Default.Type)
            .ForGetter((SwingAnimation value) => value.Type),
        ExtraCodecs.PositiveInt.OptionalFieldOf("duration", Default.Duration)
            .ForGetter((SwingAnimation value) => value.Duration),
        (type, duration) => new SwingAnimation(type, duration));

    public static readonly StreamCodec<FriendlyByteBuf, SwingAnimation> StreamCodec = StreamCodecs.Composite(
        SwingAnimationTypeExtensions.StreamCodec, value => value.Type,
        ByteBufCodecs.VarInt, value => value.Duration,
        (type, duration) => new SwingAnimation(type, duration));
}

//Weapon combat stats of an item, maps to vanilla net.minecraft.world.item.component.Weapon
public sealed record Weapon(int ItemDamagePerAttack, float DisableBlockingForSeconds)
{
    public const float AxeDisablesBlockingForSeconds = 5.0f;

    public static readonly Codec<Weapon> Codec = RecordCodecBuilder.Of2(
        ExtraCodecs.NonNegativeInt.OptionalFieldOf("item_damage_per_attack", 1)
            .ForGetter((Weapon value) => value.ItemDamagePerAttack),
        Codecs.Float.OptionalFieldOf("disable_blocking_for_seconds", 0f)
            .ForGetter((Weapon value) => value.DisableBlockingForSeconds),
        (damage, disable) => new Weapon(damage, disable));

    public static readonly StreamCodec<NacreCraft.Network.RegistryFriendlyByteBuf, Weapon> StreamCodec =
        StreamCodecs.Composite(ByteBufCodecs.VarInt, value => value.ItemDamagePerAttack,
            ByteBufCodecs.Float, value => value.DisableBlockingForSeconds,
            (damage, disable) => new Weapon(damage, disable));

    public Weapon(int damagePerAttack) : this(damagePerAttack, 0f)
    {
    }
}
