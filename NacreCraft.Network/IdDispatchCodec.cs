namespace NacreCraft.Network;

//IdDispatchCodec dispatches values by a VarInt id chosen from the value's type, maps to vanilla net.minecraft.network.codec.IdDispatchCodec
//B is the buffer type, V the value, T the type key; the id table is fixed when the builder runs
public sealed class IdDispatchCodec<B, V, T> : StreamCodec<B, V>
    where B : FriendlyByteBuf
    where T : notnull
{
    private readonly Func<V, T> _typeGetter;
    private readonly Entry[] _byId;
    private readonly Dictionary<T, int> _toId;

    private IdDispatchCodec(Func<V, T> typeGetter, Entry[] byId, Dictionary<T, int> toId)
    {
        _typeGetter = typeGetter;
        _byId = byId;
        _toId = toId;
    }

    internal static IdDispatchCodec<B, V, T> Create(Func<V, T> typeGetter, Entry[] byId, Dictionary<T, int> toId)
        => new(typeGetter, byId, toId);

    public V Decode(B input)
    {
        int id = input.ReadVarInt();
        if (id < 0 || id >= _byId.Length)
            throw new IOException($"Received unknown packet id {id}");

        var entry = _byId[id];
        try
        {
            return entry.Serializer.Decode(input);
        }
        catch (Exception e)
        {
            throw new IOException($"Failed to decode packet '{entry.Type}'", e);
        }
    }

    public void Encode(B output, V value)
    {
        var type = _typeGetter(value);
        if (!_toId.TryGetValue(type, out var id))
            throw new IOException($"Sending unknown packet '{type}'");

        output.WriteVarInt(id);
        var entry = _byId[id];
        try
        {
            entry.Serializer.Encode(output, value);
        }
        catch (Exception e)
        {
            throw new IOException($"Failed to encode packet '{type}'", e);
        }
    }

    public static IdDispatchCodecBuilder<B, V, T> Builder(Func<V, T> typeGetter) => new(typeGetter);

    //Entry one registered type and its serializer; the array index is the wire id
    internal readonly record struct Entry(StreamCodec<B, V> Serializer, T Type);
}

//IdDispatchCodecBuilder collects the registrations and assigns ids in registration order
public sealed class IdDispatchCodecBuilder<B, V, T>
    where B : FriendlyByteBuf
    where T : notnull
{
    private readonly List<IdDispatchCodec<B, V, T>.Entry> _entries = new();
    private readonly Func<V, T> _typeGetter;

    internal IdDispatchCodecBuilder(Func<V, T> typeGetter) => _typeGetter = typeGetter;

    public IdDispatchCodecBuilder<B, V, T> Add(T type, StreamCodec<B, V> serializer)
    {
        _entries.Add(new IdDispatchCodec<B, V, T>.Entry(serializer, type));
        return this;
    }

    public IdDispatchCodec<B, V, T> Build()
    {
        var toId = new Dictionary<T, int>();
        foreach (var entry in _entries)
        {
            var id = toId.Count;
            if (!toId.TryAdd(entry.Type, id))
                throw new InvalidOperationException($"Duplicate registration for type {entry.Type}");
        }
        return IdDispatchCodec<B, V, T>.Create(_typeGetter, _entries.ToArray(), toId);
    }
}
