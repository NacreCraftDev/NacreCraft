namespace NacreCraft.Network;

//StreamCodecExtensions derived codecs, maps to the default methods on vanilla net.minecraft.network.codec.StreamCodec
//C# has no interface instance methods, so what vanilla writes as default methods lives here as extensions
public static class StreamCodecExtensions
{
    //Of builds a codec from a separate encoder and decoder
    public static StreamCodec<B, V> Of<B, V>(StreamEncoder<B, V> encoder, StreamDecoder<B, V> decoder)
        where B : class
        => new FuncCodec<B, V>(buf => decoder.Decode(buf), (buf, value) => encoder.Encode(buf, value));

    //OfMember builds a codec from a member encoder that takes the buffer from the value, and a decoder
    public static StreamCodec<B, V> OfMember<B, V>(StreamMemberEncoder<B, V> encoder, StreamDecoder<B, V> decoder)
        where B : class
        => new FuncCodec<B, V>(buf => decoder.Decode(buf), (buf, value) => encoder.Encode(value, buf));

    //Unit codec that writes nothing at all and rejects any value other than the fixed one
    public static StreamCodec<B, V> Unit<B, V>(V instance) where B : class
        => new FuncCodec<B, V>(
            _ => instance,
            (_, value) =>
            {
                if (!EqualityComparer<V>.Default.Equals(value, instance))
                    throw new InvalidOperationException($"Can't encode '{value}', expected '{instance}'");
            });

    //Map converts a codec of V into a codec of O through a pair of conversions
    public static StreamCodec<B, O> Map<B, V, O>(this StreamCodec<B, V> codec, Func<V, O> to, Func<O, V> from)
        where B : class
        => new FuncCodec<B, O>(buf => to(codec.Decode(buf)), (buf, value) => codec.Encode(buf, from(value)));

    //MapStream rebases a codec onto a stream type that can be converted to the original one
    public static StreamCodec<O, V> MapStream<B, O, V>(this StreamCodec<B, V> codec, Func<O, B> operation)
        where B : class
        where O : class
        => new FuncCodec<O, V>(buf => codec.Decode(operation(buf)), (buf, value) => codec.Encode(operation(buf), value));

    //Cast widens the buffer type, maps to vanilla StreamCodec.cast
    public static StreamCodec<S, V> Cast<B, S, V>(this StreamCodec<B, V> codec)
        where B : class
        where S : class, B
        => new FuncCodec<S, V>(buf => codec.Decode(buf), (buf, value) => codec.Encode(buf, value));

    //Dispatch writes a type marker with the outer codec, then delegates the body to the codec that marker selects
    public static StreamCodec<B, U> Dispatch<B, V, U>(this StreamCodec<B, V> codec, Func<U, V> type, Func<V, StreamCodec<B, U>> valueCodec)
        where B : class
        => new FuncCodec<B, U>(
            buf =>
            {
                var key = codec.Decode(buf);
                return valueCodec(key).Decode(buf);
            },
            (buf, value) =>
            {
                var key = type(value);
                codec.Encode(buf, key);
                valueCodec(key).Encode(buf, value);
            });

    //Recursive resolves a codec that refers to itself, the factory is called once on first use
    public static StreamCodec<B, V> Recursive<B, V>(Func<StreamCodec<B, V>, StreamCodec<B, V>> factory)
        where B : class
    {
        StreamCodec<B, V>? inner = null;
        StreamCodec<B, V>? self = null;
        self = new FuncCodec<B, V>(
            buf => Resolve().Decode(buf),
            (buf, value) => Resolve().Encode(buf, value));
        return self;

        //Resolve builds the inner codec on first use, by which point self is assigned
        StreamCodec<B, V> Resolve() => inner ??= factory(self!);
    }
}
