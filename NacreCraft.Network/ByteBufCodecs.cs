using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Network;

//ByteBufCodecs stream codec utilities, maps to vanilla net.minecraft.network.codec.ByteBufCodecs
//Provides the built-in scalar, array, collection and registry codecs
public static class ByteBufCodecs
{
    //Cap on the initial collection capacity to avoid preallocating huge memory on malicious large length prefixes
    public const int MaxInitialCollectionSize = 65536;

    //Bool codec, one byte, maps to vanilla BOOL
    public static readonly StreamCodec<FriendlyByteBuf, bool> Bool =
        new FuncCodec<FriendlyByteBuf, bool>(buf => buf.ReadBoolean(), (buf, value) => buf.WriteBoolean(value));

    //Byte codec, maps to vanilla BYTE
    public static readonly StreamCodec<FriendlyByteBuf, byte> Byte =
        new FuncCodec<FriendlyByteBuf, byte>(buf => buf.ReadByte(), (buf, value) => buf.WriteByte(value));

    //Short codec, maps to vanilla SHORT
    public static readonly StreamCodec<FriendlyByteBuf, short> Short =
        new FuncCodec<FriendlyByteBuf, short>(buf => buf.ReadShort(), (buf, value) => buf.WriteShort(value));

    //UnsignedShort codec widened to int, maps to vanilla UNSIGNED_SHORT
    public static readonly StreamCodec<FriendlyByteBuf, int> UnsignedShort =
        new FuncCodec<FriendlyByteBuf, int>(buf => buf.ReadUnsignedShort(), (buf, value) => buf.WriteShort(unchecked((short)value)));

    //Int codec, big-endian 4 bytes, maps to vanilla INT
    public static readonly StreamCodec<FriendlyByteBuf, int> Int =
        new FuncCodec<FriendlyByteBuf, int>(buf => buf.ReadInt(), (buf, value) => buf.WriteInt(value));

    //VarInt variable-length int codec, maps to vanilla VAR_INT
    public static readonly StreamCodec<FriendlyByteBuf, int> VarInt =
        new FuncCodec<FriendlyByteBuf, int>(buf => buf.ReadVarInt(), (buf, value) => buf.WriteVarInt(value));

    //Long codec, big-endian 8 bytes, maps to vanilla LONG
    public static readonly StreamCodec<FriendlyByteBuf, long> Long =
        new FuncCodec<FriendlyByteBuf, long>(buf => buf.ReadLong(), (buf, value) => buf.WriteLong(value));

    //VarLong variable-length long codec, maps to vanilla VAR_LONG
    public static readonly StreamCodec<FriendlyByteBuf, long> VarLong =
        new FuncCodec<FriendlyByteBuf, long>(buf => buf.ReadVarLong(), (buf, value) => buf.WriteVarLong(value));

    //Float float codec, big-endian 4 bytes, maps to vanilla FLOAT
    public static readonly StreamCodec<FriendlyByteBuf, float> Float =
        new FuncCodec<FriendlyByteBuf, float>(buf => buf.ReadFloat(), (buf, value) => buf.WriteFloat(value));

    //Double double codec, big-endian 8 bytes, maps to vanilla DOUBLE
    public static readonly StreamCodec<FriendlyByteBuf, double> Double =
        new FuncCodec<FriendlyByteBuf, double>(buf => buf.ReadDouble(), (buf, value) => buf.WriteDouble(value));

    //ByteArray codec, VarInt length prefix, maps to vanilla BYTE_ARRAY
    public static readonly StreamCodec<FriendlyByteBuf, byte[]> ByteArray =
        new FuncCodec<FriendlyByteBuf, byte[]>(buf => buf.ReadByteArray(), (buf, value) => buf.WriteByteArray(value));

    //LongArray codec, VarInt length prefix, maps to vanilla LONG_ARRAY
    public static readonly StreamCodec<FriendlyByteBuf, long[]> LongArray =
        new FuncCodec<FriendlyByteBuf, long[]>(buf => buf.ReadLongArray(), (buf, value) => buf.WriteLongArray(value));

    //Tag codec for a rootless NBT payload, maps to vanilla TAG; an end tag is rejected on both directions
    public static readonly StreamCodec<FriendlyByteBuf, NacreCraft.Nbt.Tag> Tag =
        new FuncCodec<FriendlyByteBuf, NacreCraft.Nbt.Tag>(
            buf => buf.ReadNbt() is { } tag && tag is not NacreCraft.Nbt.EndTag
                ? tag
                : throw new InvalidOperationException("Expected non-null compound tag"),
            (buf, value) =>
            {
                if (value is NacreCraft.Nbt.EndTag)
                    throw new InvalidOperationException("Expected non-null compound tag");
                buf.WriteNbt(value);
            });

    //CompoundTag codec, maps to vanilla COMPOUND_TAG
    public static readonly StreamCodec<FriendlyByteBuf, NacreCraft.Nbt.CompoundTag> CompoundTag =
        Tag.Map(
            tag => tag as NacreCraft.Nbt.CompoundTag
                ?? throw new InvalidOperationException($"Not a compound tag: {tag}"),
            compound => compound);

    //OptionalCompoundTag codec, a false flag writes no payload, maps to vanilla OPTIONAL_COMPOUND_TAG
    public static readonly StreamCodec<FriendlyByteBuf, Optional<NacreCraft.Nbt.CompoundTag>> OptionalCompoundTag =
        new FuncCodec<FriendlyByteBuf, Optional<NacreCraft.Nbt.CompoundTag>>(
            buf => Optional<NacreCraft.Nbt.CompoundTag>.OfNullable(buf.ReadNbt() as NacreCraft.Nbt.CompoundTag),
            (buf, value) => buf.WriteNbt(value.IsPresent ? value.Get() : null));

    //StringUtf8 codec for UTF-8 strings with a variable-length prefix; maxLength is the length cap check
    public static StreamCodec<FriendlyByteBuf, string> StringUtf8(int maxLength = 32767)
        => new StringStreamCodec(maxLength);

    //ByteArray codec with a maximum length, maps to vanilla byteArray(maxSize)
    public static StreamCodec<FriendlyByteBuf, byte[]> ByteArrayOf(int maxSize)
        => new FuncCodec<FriendlyByteBuf, byte[]>(
            buf => buf.ReadByteArray(maxSize),
            (buf, value) =>
            {
                if (value.Length > maxSize)
                    throw new InvalidOperationException($"ByteArray with size {value.Length} is bigger than allowed {maxSize}");
                buf.WriteByteArray(value);
            });

    //Identifier codec for namespace identifiers, maps to Identifier in vanilla ByteBufCodecs
    public static StreamCodec<RegistryFriendlyByteBuf, Identifier> Identifier()
        => new IdentifierStreamCodec();

    //Optional codec writes a presence flag followed by the value, maps to vanilla optional(codec)
    public static StreamCodec<B, Optional<V>> Optional<B, V>(StreamCodec<B, V> elementCodec) where B : FriendlyByteBuf
        => new FuncCodec<B, Optional<V>>(
            buf => buf.ReadBoolean() ? Optional<V>.Of(elementCodec.Decode(buf)) : Optional<V>.Empty(),
            (buf, value) =>
            {
                buf.WriteBoolean(value.IsPresent);
                if (value.IsPresent) elementCodec.Encode(buf, value.Get());
            });

    //IdMapper codec reads a VarInt id and maps it through the given functions, maps to vanilla idMapper
    public static StreamCodec<FriendlyByteBuf, T> IdMapper<T>(Func<int, T> byId, Func<T, int> toId)
        => new FuncCodec<FriendlyByteBuf, T>(buf => byId(buf.ReadVarInt()), (buf, value) => buf.WriteVarInt(toId(value)));

    //Registry codec writes a registry id, maps to vanilla registry(registryKey)
    public static StreamCodec<RegistryFriendlyByteBuf, T> RegistryValue<T>(ResourceKey<Registry<T>> registryKey) where T : class
        => new RegistryValueStreamCodec<T>(registryKey);

    //ReadCount reads a VarInt count and enforces the maximum, maps to vanilla readCount
    public static int ReadCount(FriendlyByteBuf buf, int maxSize)
    {
        var count = buf.ReadVarInt();
        if (count > maxSize)
            throw new InvalidOperationException($"{count} elements exceeded max size of: {maxSize}");
        return count;
    }

    //WriteCount writes a VarInt count and enforces the maximum, maps to vanilla writeCount
    public static void WriteCount(FriendlyByteBuf buf, int count, int maxSize)
    {
        if (count > maxSize)
            throw new InvalidOperationException($"{count} elements exceeded max size of: {maxSize}");
        buf.WriteVarInt(count);
    }

    //Collection codec: VarInt length prefix + element list
    //elementCodec is the per-element codec; maxSize is the length cap check
    public static StreamCodec<B, List<V>> Collection<B, V>(StreamCodec<B, V> elementCodec, int maxSize = int.MaxValue) where B : class
        => new CollectionStreamCodec<B, V>(elementCodec, maxSize);

    //Collection codec with a caller-supplied container factory, maps to vanilla collection(constructor, codec, maxSize)
    public static StreamCodec<B, C> Collection<B, V, C>(Func<int, C> constructor, StreamCodec<B, V> elementCodec, int maxSize = int.MaxValue)
        where B : FriendlyByteBuf
        where C : ICollection<V>
        => new FuncCodec<B, C>(
            buf =>
            {
                var count = ReadCount(buf, maxSize);
                var result = constructor(Math.Min(count, MaxInitialCollectionSize));
                for (var i = 0; i < count; i++) result.Add(elementCodec.Decode(buf));
                return result;
            },
            (buf, value) =>
            {
                WriteCount(buf, value.Count, maxSize);
                foreach (var element in value) elementCodec.Encode(buf, element);
            });

    //List codec with a VarInt length prefix, maps to vanilla list()
    public static StreamCodec<B, List<V>> ListOf<B, V>(StreamCodec<B, V> elementCodec, int maxSize = int.MaxValue) where B : FriendlyByteBuf
        => Collection<B, V, List<V>>(capacity => new List<V>(capacity), elementCodec, maxSize);

    //Map codec with a VarInt entry count, maps to vanilla map(constructor, keyCodec, valueCodec, maxSize)
    public static StreamCodec<B, Dictionary<K, V>> MapOf<B, K, V>(StreamCodec<B, K> keyCodec, StreamCodec<B, V> valueCodec, int maxSize = int.MaxValue)
        where B : FriendlyByteBuf
        where K : notnull
        => new FuncCodec<B, Dictionary<K, V>>(
            buf =>
            {
                var count = ReadCount(buf, maxSize);
                var result = new Dictionary<K, V>(Math.Min(count, MaxInitialCollectionSize));
                for (var i = 0; i < count; i++) result[keyCodec.Decode(buf)] = valueCodec.Decode(buf);
                return result;
            },
            (buf, value) =>
            {
                WriteCount(buf, value.Count, maxSize);
                foreach (var (key, element) in value)
                {
                    keyCodec.Encode(buf, key);
                    valueCodec.Encode(buf, element);
                }
            });

    //Holder codec reads an id from RegistryFriendlyByteBuf and turns it into a Holder.Reference
    //registryKey identifies the target registry; encode looks up the id by value and decode takes the Reference by id
    public static StreamCodec<RegistryFriendlyByteBuf, Holder<T>> Holder<T>(ResourceKey<Registry<T>> registryKey) where T : class
        => new HolderStreamCodec<T>(registryKey);

    //Holder codec with directCodec supports a wrapped Direct value: id==0 goes through directCodec, otherwise id-1 goes through the registry Reference
    //Maps to vanilla ByteBufCodecs.holder(registryKey, directCodec)
    public static StreamCodec<RegistryFriendlyByteBuf, Holder<T>> Holder<T>(
        ResourceKey<Registry<T>> registryKey,
        StreamCodec<RegistryFriendlyByteBuf, T> directCodec) where T : class
        => new DirectHolderStreamCodec<T>(registryKey, directCodec);
}

//StringStreamCodec string codec, maps to vanilla ByteBufCodecs.stringUtf8
internal sealed class StringStreamCodec : StreamCodec<FriendlyByteBuf, string>
{
    private readonly int _maxLength;

    public StringStreamCodec(int maxLength) => _maxLength = maxLength;

    public string Decode(FriendlyByteBuf buf) => buf.ReadString(_maxLength);

    public void Encode(FriendlyByteBuf buf, string value) => buf.WriteString(value, _maxLength);
}

//IdentifierStreamCodec codec for namespace identifiers, encoded as a namespace:path string
internal sealed class IdentifierStreamCodec : StreamCodec<RegistryFriendlyByteBuf, Identifier>
{
    public Identifier Decode(RegistryFriendlyByteBuf buf) => buf.ReadIdentifier();

    public void Encode(RegistryFriendlyByteBuf buf, Identifier value) => buf.WriteIdentifier(value);
}

//RegistryValueStreamCodec codec between a registry id and the registry value, maps to vanilla ByteBufCodecs.registry(registryKey)
internal sealed class RegistryValueStreamCodec<T> : StreamCodec<RegistryFriendlyByteBuf, T> where T : class
{
    private readonly ResourceKey<Registry<T>> _registryKey;

    public RegistryValueStreamCodec(ResourceKey<Registry<T>> registryKey) => _registryKey = registryKey;

    public T Decode(RegistryFriendlyByteBuf buf)
        => buf.Lookup(_registryKey).ByIdOrThrow(buf.ReadVarInt());

    public void Encode(RegistryFriendlyByteBuf buf, T value)
        => buf.WriteVarInt(buf.Lookup(_registryKey).GetIdOrThrow(value));
}

//HolderStreamCodec bidirectional codec between registry id and Holder.Reference
internal sealed class HolderStreamCodec<T> : StreamCodec<RegistryFriendlyByteBuf, Holder<T>> where T : class
{
    private readonly ResourceKey<Registry<T>> _registryKey;

    public HolderStreamCodec(ResourceKey<Registry<T>> registryKey)
    {
        _registryKey = registryKey;
    }

    public Holder<T> Decode(RegistryFriendlyByteBuf buf)
    {
        int id = buf.ReadVarInt();
        var registry = buf.Lookup(_registryKey);
        var holder = registry.Get(id);
        if (holder is null)
            throw new InvalidOperationException($"unknown holder id {id} in {_registryKey}");
        return holder;
    }

    public void Encode(RegistryFriendlyByteBuf buf, Holder<T> value)
    {
        if (value is not Reference<T> reference)
            throw new InvalidOperationException($"cannot encode non-Reference holder: {value}");
        var registry = buf.Lookup(_registryKey);
        int id = registry.GetId(reference.Value);
        if (id == IdMap<T>.Default)
            throw new InvalidOperationException($"holder value not registered: {reference.Value}");
        buf.WriteVarInt(id);
    }
}

//DirectHolderStreamCodec mixed codec for registry id + wrapped Direct value, maps to vanilla ByteBufCodecs.holder(registryKey, directCodec)
//DIRECT_HOLDER_ID = 0 is reserved for Direct; Reference writes id+1, and on decode id==0 goes through directCodec, otherwise id-1 looks up the registry
internal sealed class DirectHolderStreamCodec<T> : StreamCodec<RegistryFriendlyByteBuf, Holder<T>> where T : class
{
    private const int DirectHolderId = 0;

    private readonly ResourceKey<Registry<T>> _registryKey;
    private readonly StreamCodec<RegistryFriendlyByteBuf, T> _directCodec;

    public DirectHolderStreamCodec(ResourceKey<Registry<T>> registryKey, StreamCodec<RegistryFriendlyByteBuf, T> directCodec)
    {
        _registryKey = registryKey;
        _directCodec = directCodec;
    }

    public Holder<T> Decode(RegistryFriendlyByteBuf buf)
    {
        int id = buf.ReadVarInt();
        if (id == DirectHolderId)
            return Holder<T>.Direct(_directCodec.Decode(buf));
        var registry = buf.Lookup(_registryKey);
        var holder = registry.Get(id - 1);
        if (holder is null)
            throw new InvalidOperationException($"unknown holder id {id} in {_registryKey}");
        return holder;
    }

    public void Encode(RegistryFriendlyByteBuf buf, Holder<T> value)
    {
        if (value.HolderKind == Holder<T>.Kind.Reference)
        {
            var registry = buf.Lookup(_registryKey);
            int id = registry.GetId(value.Value);
            if (id == IdMap<T>.Default)
                throw new InvalidOperationException($"holder value not registered: {value.Value}");
            buf.WriteVarInt(id + 1);
        }
        else
        {
            buf.WriteVarInt(DirectHolderId);
            _directCodec.Encode(buf, value.Value);
        }
    }
}

//CollectionStreamCodec collection codec with a VarInt length prefix, encoding and decoding element by element
internal sealed class CollectionStreamCodec<B, V> : StreamCodec<B, List<V>> where B : class
{
    private readonly StreamCodec<B, V> _elementCodec;
    private readonly int _maxSize;

    public CollectionStreamCodec(StreamCodec<B, V> elementCodec, int maxSize)
    {
        _elementCodec = elementCodec;
        _maxSize = maxSize;
    }

    public List<V> Decode(B buf)
    {
        int size = ReadVarInt(buf);
        if (size > _maxSize)
            throw new InvalidOperationException($"collection length out of range {size} > {_maxSize}");
        var list = new List<V>(Math.Min(size, ByteBufCodecs.MaxInitialCollectionSize));
        for (int i = 0; i < size; i++)
            list.Add(_elementCodec.Decode(buf));
        return list;
    }

    public void Encode(B buf, List<V> value)
    {
        if (value.Count > _maxSize)
            throw new InvalidOperationException($"collection length out of range {value.Count} > {_maxSize}");
        WriteVarInt(buf, value.Count);
        foreach (var item in value)
            _elementCodec.Encode(buf, item);
    }

    //ReadVarInt reads a VarInt from FriendlyByteBuf via reflection to avoid binding type B to FriendlyByteBuf
    //In practice B is always a FriendlyByteBuf subclass, so its ReadVarInt method is called
    private static int ReadVarInt(B buf)
    {
        if (buf is FriendlyByteBuf fbb) return fbb.ReadVarInt();
        throw new InvalidOperationException($"unsupported buffer type {buf?.GetType()}");
    }

    private static void WriteVarInt(B buf, int value)
    {
        if (buf is FriendlyByteBuf fbb) { fbb.WriteVarInt(value); return; }
        throw new InvalidOperationException($"unsupported buffer type {buf?.GetType()}");
    }
}
