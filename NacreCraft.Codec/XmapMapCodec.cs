namespace NacreCraft.Codec;

//XmapMapCodec maps a codec between two representations, the C# stand-in for the vanilla MapCodec.xmap
//Useful where a registry holds a common interface but each implementation owns its concrete codec
public sealed class XmapMapCodec<TA, TB>(MapCodec<TA> inner, Func<TA, TB> to, Func<TB, TA> from) : MapCodec<TB>
{
    public DataResult<TB> Decode<U>(DynamicOps<U> ops, MapLike<U> input) => inner.Decode(ops, input).Map(to);

    public DataResult<U> EncodeStart<U>(DynamicOps<U> ops, TB value) => inner.EncodeStart(ops, from(value));

    public RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, TB value, RecordBuilder<U> builder)
        => inner.EncodeTo(ops, from(value), builder);

    public RecordBuilder<U> Encoder<U>(DynamicOps<U> ops) => inner.Encoder(ops);
}
