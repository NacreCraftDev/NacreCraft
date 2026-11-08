using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.ConsumeEffects;

//PlaySoundConsumeEffect plays a sound when the item is consumed, maps to vanilla PlaySoundConsumeEffect
//NC sounds are plain values rather than registry entries, so the codec stores the sound id directly instead of a Holder<SoundEvent>
public sealed class PlaySoundConsumeEffect : ConsumeEffect
{
    //SoundCodec sound id codec, maps loosely to vanilla SoundEvent.CODEC
    public static readonly Codec<SoundEvent> SoundCodec = IdentifierCodec.Instance.ComapFlatMap(
        id => DataResult<SoundEvent>.Success(new SoundEvent(id)),
        sound => sound.Location);

    //Codec persistence codec, the sound field, maps to vanilla CODEC
    public static readonly Codec<PlaySoundConsumeEffect> Codec = RecordCodecBuilder.Of1(
        SoundCodec.FieldOf("sound").ForGetter((PlaySoundConsumeEffect effect) => effect.Sound),
        sound => new PlaySoundConsumeEffect(sound));

    //StreamCodec network codec, the sound id, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<RegistryFriendlyByteBuf, PlaySoundConsumeEffect> StreamCodec =
        ByteBufCodecs.Identifier()
            .Map(id => new PlaySoundConsumeEffect(new SoundEvent(id)), effect => effect.Sound.Location);

    public static readonly ConsumeEffectType<PlaySoundConsumeEffect> EffectType =
        ConsumeEffectType.Register(Identifier.WithDefaultNamespace("play_sound"),
            new SimpleConsumeEffectType<PlaySoundConsumeEffect>(
                Identifier.WithDefaultNamespace("play_sound"), Codec, StreamCodec));

    public PlaySoundConsumeEffect(SoundEvent sound) => Sound = sound;

    //Sound the sound event this consume effect plays
    public SoundEvent Sound { get; }

    public override ConsumeEffectType Type => EffectType;

    //Apply plays the sound at the consumer's position and always reports success, maps to vanilla apply
    public override bool Apply(ServerLevel level, ItemStack stack, LivingEntity user)
    {
        level.PlaySound(Sound, user.GetSoundSource(), user.BlockPosition(), 1f, 1f);
        return true;
    }

    public override string ToString() => $"PlaySoundConsumeEffect[sound={Sound.Location}]";
}
