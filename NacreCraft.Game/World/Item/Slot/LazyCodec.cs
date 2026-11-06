using NacreCraft.Codec;

namespace NacreCraft.Game.World.Items.Slots;

//LazyCodec resolves its target on first use, so a codec may refer to one that is still being constructed
//
//SlotSources.CODEC and GroupSlotSource.INLINE_CODEC refer to each other. With both as eager static fields, whichever
//type initializes first reads the other's field before it is assigned and the slot source codecs stay unusable; this
//wrapper lets the static constructors build their shells and only resolve the cycle when a parse actually happens.
internal sealed class LazyCodec<T> : ScalarCodec<T>
{
    private readonly Func<Codec<T>> _factory;
    private Codec<T>? _resolved;

    public LazyCodec(Func<Codec<T>> factory)
    {
        _factory = factory;
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, T value) => Resolve().EncodeStart(ops, value);

    public override DataResult<T> Parse<U>(DynamicOps<U> ops, U input) => Resolve().Parse(ops, input);

    private Codec<T> Resolve() => _resolved ??= _factory();
}
