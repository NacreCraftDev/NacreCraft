using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items.ConsumeEffects;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.Component;

//Consumable how an item is consumed, maps to vanilla net.minecraft.world.item.component.Consumable
//NC stores the completion sound as a value rather than a registry holder because built-in sound events are plain values here
public sealed record Consumable(
    float ConsumeSeconds,
    ItemUseAnimation Animation,
    SoundEvent Sound,
    bool HasConsumeParticles,
    IReadOnlyList<ConsumeEffect> OnConsumeEffects)
{
    public const float DefaultConsumeSeconds = 1.6f;

    public static readonly Codec<Consumable> Codec = RecordCodecBuilder.Of5(
        Codecs.Float.OptionalFieldOf("consume_seconds", DefaultConsumeSeconds)
            .ForGetter((Consumable value) => value.ConsumeSeconds),
        ItemUseAnimationExtensions.Codec.OptionalFieldOf("animation", ItemUseAnimation.Eat)
            .ForGetter((Consumable value) => value.Animation),
        SoundEventCodec.Instance.OptionalFieldOf("sound", SoundEvents.GenericEat)
            .ForGetter((Consumable value) => value.Sound),
        Codecs.Bool.OptionalFieldOf("has_consume_particles", true)
            .ForGetter((Consumable value) => value.HasConsumeParticles),
        ConsumeEffect.Codec.ListOf().OptionalFieldOf("on_consume_effects", Array.Empty<ConsumeEffect>())
            .ForGetter((Consumable value) => value.OnConsumeEffects),
        (seconds, animation, sound, particles, effects)
            => new Consumable(seconds, animation, sound, particles, effects));

    private static readonly StreamCodec<FriendlyByteBuf, SoundEvent> SoundStreamCodec =
        ItemComponentCodecs.IdentifierStreamCodec.Map(id => new SoundEvent(id), sound => sound.Location);

    public static readonly StreamCodec<RegistryFriendlyByteBuf, Consumable> StreamCodec = StreamCodecs.Composite(
        ByteBufCodecs.Float, value => value.ConsumeSeconds,
        ItemUseAnimationExtensions.StreamCodec, value => value.Animation,
        SoundStreamCodec, value => value.Sound,
        ByteBufCodecs.Bool, value => value.HasConsumeParticles,
        ByteBufCodecs.ListOf(ConsumeEffect.StreamCodec), value => value.OnConsumeEffects.ToList(),
        (seconds, animation, sound, particles, effects)
            => new Consumable(seconds, animation, sound, particles, effects));

    //ConsumeTicks how long the use action takes, maps to vanilla consumeTicks
    public int ConsumeTicks => (int)(ConsumeSeconds * 20f);

    public static Builder CreateBuilder() => new();

    //Builder fluent construction used by the default consumable presets, maps to vanilla Consumable.Builder
    public sealed class Builder
    {
        private float _consumeSeconds = DefaultConsumeSeconds;
        private ItemUseAnimation _animation = ItemUseAnimation.Eat;
        private SoundEvent _sound = SoundEvents.GenericEat;
        private bool _hasConsumeParticles = true;
        private readonly List<ConsumeEffect> _effects = new();

        public Builder ConsumeSeconds(float seconds)
        {
            _consumeSeconds = seconds;
            return this;
        }

        public Builder Animation(ItemUseAnimation animation)
        {
            _animation = animation;
            return this;
        }

        public Builder Sound(SoundEvent sound)
        {
            _sound = sound;
            return this;
        }

        public Builder HasConsumeParticles(bool particles)
        {
            _hasConsumeParticles = particles;
            return this;
        }

        public Builder OnConsume(ConsumeEffect effect)
        {
            _effects.Add(effect);
            return this;
        }

        public Consumable Build()
            => new(_consumeSeconds, _animation, _sound, _hasConsumeParticles, _effects.ToArray());
    }
}

//ConsumableListener a component that reacts to an item being consumed, maps to vanilla net.minecraft.world.item.component.ConsumableListener
public interface ConsumableListener
{
    void OnConsume(ServerLevel level, LivingEntity user, ItemStack stack, Consumable consumable);
}
