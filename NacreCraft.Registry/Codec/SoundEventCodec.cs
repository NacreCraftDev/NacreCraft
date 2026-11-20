using NacreCraft.Codec;

namespace NacreCraft.Registry.Codec;

//SoundEventCodec sound event value codec, maps loosely to vanilla SoundEvent.CODEC
//NC sounds are plain values, so the codec stores the sound id directly instead of a registry holder
public sealed class SoundEventCodec : ScalarCodec<SoundEvent>
{
    public static readonly SoundEventCodec Instance = new();

    public override DataResult<SoundEvent> Parse<U>(DynamicOps<U> ops, U input)
        => IdentifierCodec.Instance.Parse(ops, input).Map(id => new SoundEvent(id));

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, SoundEvent value)
        => IdentifierCodec.Instance.EncodeStart(ops, value.Location);
}
