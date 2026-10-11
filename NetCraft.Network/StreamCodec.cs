namespace NetCraft.Network;

//StreamCodec streaming codec interface, maps to vanilla net.minecraft.network.codec.StreamCodec
//B is the buffer type and V is the value type; both encode and decode are inherited from the halves
//B is marked in (contravariant) so StreamCodec<FriendlyByteBuf, V> implicitly converts to StreamCodec<RegistryFriendlyByteBuf, V>
public interface StreamCodec<in B, V> : StreamEncoder<B, V>, StreamDecoder<B, V>
{
}

//IObjectEncodable non-generic codec interface supporting encoding as object
//common packets have the static type Packet<parent listener> and cannot be cast to Packet<child listener>, so the encode chain must pass them as object
internal interface IObjectEncodable
{
    //EncodeObject encodes a packet as object, extracting by its actual type inside the implementation
    void EncodeObject(RegistryFriendlyByteBuf buf, object packet);
}

//StreamCodecs static factory class providing basic codecs
public static class StreamCodecs
{
    //Bool bool codec reading a 1-byte boolean
    public static StreamCodec<T, bool> Bool<T>(Func<T, bool> reader, Action<T, bool> writer)
        where T : class
        => new FuncCodec<T, bool>(reader, writer);

    //Byte byte codec reading 1 byte
    public static StreamCodec<T, byte> Byte<T>(Func<T, byte> reader, Action<T, byte> writer)
        where T : class
        => new FuncCodec<T, byte>(reader, writer);

    //Int int codec, big-endian 4 bytes
    public static StreamCodec<T, int> Int<T>(Func<T, int> reader, Action<T, int> writer)
        where T : class
        => new FuncCodec<T, int>(reader, writer);

    //VarInt variable-length int codec
    public static StreamCodec<T, int> VarInt<T>(Func<T, int> reader, Action<T, int> writer)
        where T : class
        => new FuncCodec<T, int>(reader, writer);

    //Long long codec, big-endian 8 bytes
    public static StreamCodec<T, long> Long<T>(Func<T, long> reader, Action<T, long> writer)
        where T : class
        => new FuncCodec<T, long>(reader, writer);

    //String UTF-8 string codec with a leading length prefix
    public static StreamCodec<T, string> String<T>(Func<T, string> reader, Action<T, string> writer)
        where T : class
        => new FuncCodec<T, string>(reader, writer);

    //Composite two-field codec, reads fields in order and passes them to the constructor
    //Write order matches read order, maps to vanilla StreamCodec.composite
    public static StreamCodec<B, C> Composite<B, C, T1, T2>(
        StreamCodec<B, T1> codec1, Func<C, T1> getter1,
        StreamCodec<B, T2> codec2, Func<C, T2> getter2,
        Func<T1, T2, C> constructor) where B : class
        => new FuncCodec<B, C>(
            buf => constructor(codec1.Decode(buf), codec2.Decode(buf)),
            (buf, value) =>
            {
                codec1.Encode(buf, getter1(value));
                codec2.Encode(buf, getter2(value));
            });

    //Composite three-field codec
    public static StreamCodec<B, C> Composite<B, C, T1, T2, T3>(
        StreamCodec<B, T1> codec1, Func<C, T1> getter1,
        StreamCodec<B, T2> codec2, Func<C, T2> getter2,
        StreamCodec<B, T3> codec3, Func<C, T3> getter3,
        Func<T1, T2, T3, C> constructor) where B : class
        => new FuncCodec<B, C>(
            buf => constructor(codec1.Decode(buf), codec2.Decode(buf), codec3.Decode(buf)),
            (buf, value) =>
            {
                codec1.Encode(buf, getter1(value));
                codec2.Encode(buf, getter2(value));
                codec3.Encode(buf, getter3(value));
            });

    //Composite four-field codec
    public static StreamCodec<B, C> Composite<B, C, T1, T2, T3, T4>(
        StreamCodec<B, T1> codec1, Func<C, T1> getter1,
        StreamCodec<B, T2> codec2, Func<C, T2> getter2,
        StreamCodec<B, T3> codec3, Func<C, T3> getter3,
        StreamCodec<B, T4> codec4, Func<C, T4> getter4,
        Func<T1, T2, T3, T4, C> constructor) where B : class
        => new FuncCodec<B, C>(
            buf => constructor(codec1.Decode(buf), codec2.Decode(buf), codec3.Decode(buf), codec4.Decode(buf)),
            (buf, value) =>
            {
                codec1.Encode(buf, getter1(value));
                codec2.Encode(buf, getter2(value));
                codec3.Encode(buf, getter3(value));
                codec4.Encode(buf, getter4(value));
            });

    //Composite five-field codec
    public static StreamCodec<B, C> Composite<B, C, T1, T2, T3, T4, T5>(
        StreamCodec<B, T1> codec1, Func<C, T1> getter1,
        StreamCodec<B, T2> codec2, Func<C, T2> getter2,
        StreamCodec<B, T3> codec3, Func<C, T3> getter3,
        StreamCodec<B, T4> codec4, Func<C, T4> getter4,
        StreamCodec<B, T5> codec5, Func<C, T5> getter5,
        Func<T1, T2, T3, T4, T5, C> constructor) where B : class
        => new FuncCodec<B, C>(
            buf => constructor(codec1.Decode(buf), codec2.Decode(buf), codec3.Decode(buf), codec4.Decode(buf), codec5.Decode(buf)),
            (buf, value) =>
            {
                codec1.Encode(buf, getter1(value));
                codec2.Encode(buf, getter2(value));
                codec3.Encode(buf, getter3(value));
                codec4.Encode(buf, getter4(value));
                codec5.Encode(buf, getter5(value));
            });

    //Composite six-field codec
    public static StreamCodec<B, C> Composite<B, C, T1, T2, T3, T4, T5, T6>(
        StreamCodec<B, T1> codec1, Func<C, T1> getter1,
        StreamCodec<B, T2> codec2, Func<C, T2> getter2,
        StreamCodec<B, T3> codec3, Func<C, T3> getter3,
        StreamCodec<B, T4> codec4, Func<C, T4> getter4,
        StreamCodec<B, T5> codec5, Func<C, T5> getter5,
        StreamCodec<B, T6> codec6, Func<C, T6> getter6,
        Func<T1, T2, T3, T4, T5, T6, C> constructor) where B : class
        => new FuncCodec<B, C>(
            buf => constructor(codec1.Decode(buf), codec2.Decode(buf), codec3.Decode(buf), codec4.Decode(buf), codec5.Decode(buf), codec6.Decode(buf)),
            (buf, value) =>
            {
                codec1.Encode(buf, getter1(value));
                codec2.Encode(buf, getter2(value));
                codec3.Encode(buf, getter3(value));
                codec4.Encode(buf, getter4(value));
                codec5.Encode(buf, getter5(value));
                codec6.Encode(buf, getter6(value));
            });
}

//FuncCodec functional codec implementation wrapping reader/writer delegates as a StreamCodec
internal sealed class FuncCodec<B, V> : StreamCodec<B, V> where B : class
{
    private readonly Func<B, V> _reader;
    private readonly Action<B, V> _writer;

    public FuncCodec(Func<B, V> reader, Action<B, V> writer)
    {
        _reader = reader;
        _writer = writer;
    }

    public V Decode(B buf) => _reader(buf);
    public void Encode(B buf, V value) => _writer(buf, value);
}
