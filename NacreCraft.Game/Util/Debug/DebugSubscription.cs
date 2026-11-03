using NacreCraft.Network;
using NacreCraft.Registry;

namespace NacreCraft.Game.Util.Debug;

//DebugSubscription maps to vanilla net.minecraft.util.debug.DebugSubscription
//Vanilla is generic over the value type; the registry holds the non-generic base so subscriptions with different
//payload types can share one registry, and DebugSubscription<T> adds the typed value codec on top
public class DebugSubscription
{
    //DoesNotExpire a subscription without an expiry time, maps to vanilla DOES_NOT_EXPIRE
    public const int DoesNotExpire = 0;

    //ValueCodec serializes the value of a concrete DebugSubscription<T>; null for simple subscriptions that carry no value
    public IDebugValueCodec? ValueCodec { get; }

    //ExpireAfterTicks how long a client-side value stays before it is dropped, zero means it does not expire
    public int ExpireAfterTicks { get; }

    public DebugSubscription(IDebugValueCodec? valueCodec, int expireAfterTicks)
    {
        ValueCodec = valueCodec;
        ExpireAfterTicks = expireAfterTicks;
    }

    public DebugSubscription(IDebugValueCodec? valueCodec) : this(valueCodec, DoesNotExpire) { }

    //PackUpdate wraps a present value for a block/chunk/entity update
    public Update PackUpdate(object? value) => new(this, value is not null, value);

    //EmptyUpdate clears the value on the client
    public Update EmptyUpdate() => new(this, false, null);

    //PackEvent wraps a one-shot event payload
    public Event PackEvent(object value) => new(this, value);

    public override string ToString()
        => BuiltInRegistries.DEBUG_SUBSCRIPTION.GetKey(this)?.ToString() ?? GetType().Name;
}

//DebugSubscription<T> typed subscription, maps to vanilla DebugSubscription<T>
public sealed class DebugSubscription<T> : DebugSubscription
{
    //ValueStreamCodec typed codec of this subscription's payload; null when the subscription carries no value
    public StreamCodec<RegistryFriendlyByteBuf, T>? ValueStreamCodec { get; }

    public DebugSubscription(StreamCodec<RegistryFriendlyByteBuf, T>? valueStreamCodec, int expireAfterTicks)
        : base(valueStreamCodec is null ? null : new DebugValueCodec<T>(valueStreamCodec), expireAfterTicks)
    {
        ValueStreamCodec = valueStreamCodec;
    }

    public DebugSubscription(StreamCodec<RegistryFriendlyByteBuf, T>? valueStreamCodec) : this(valueStreamCodec, DoesNotExpire) { }
}

//IDebugValueCodec non-generic view of a typed value codec, so the base subscription can encode and decode without knowing T
public interface IDebugValueCodec
{
    object Decode(RegistryFriendlyByteBuf buf);

    void Encode(RegistryFriendlyByteBuf buf, object value);
}

//DebugValueCodec<T> adapts a typed StreamCodec to the non-generic interface
internal sealed class DebugValueCodec<T> : IDebugValueCodec
{
    private readonly StreamCodec<RegistryFriendlyByteBuf, T> _codec;

    public DebugValueCodec(StreamCodec<RegistryFriendlyByteBuf, T> codec) => _codec = codec;

    public object Decode(RegistryFriendlyByteBuf buf) => _codec.Decode(buf)!;

    public void Encode(RegistryFriendlyByteBuf buf, object value) => _codec.Encode(buf, (T)value);
}

//Update a subscription value update, maps to vanilla DebugSubscription.Update
//HasValue stands in for the vanilla Optional<T>; the raw type keeps the packet code independent of T
public sealed record Update(DebugSubscription Subscription, bool HasValue, object? Value)
{
    public static StreamCodec<RegistryFriendlyByteBuf, Update> StreamCodec { get; } = new UpdateCodec();
}

//Event a subscription one-shot event, maps to vanilla DebugSubscription.Event
public sealed record Event(DebugSubscription Subscription, object Value)
{
    public static StreamCodec<RegistryFriendlyByteBuf, Event> StreamCodec { get; } = new EventCodec();
}

//UpdateCodec registry id followed by the optional value, maps to vanilla ByteBufCodecs.registry(...).dispatch(...)
internal sealed class UpdateCodec : StreamCodec<RegistryFriendlyByteBuf, Update>
{
    public Update Decode(RegistryFriendlyByteBuf buf)
    {
        var subscription = DebugSubscriptionCodec.Read(buf);
        if (subscription.ValueCodec is null) return new Update(subscription, false, null);
        var present = buf.ReadBoolean();
        return present
            ? new Update(subscription, true, subscription.ValueCodec.Decode(buf))
            : new Update(subscription, false, null);
    }

    public void Encode(RegistryFriendlyByteBuf buf, Update value)
    {
        DebugSubscriptionCodec.Write(buf, value.Subscription);
        if (value.Subscription.ValueCodec is null) return;
        buf.WriteBoolean(value.HasValue);
        if (value.HasValue) value.Subscription.ValueCodec.Encode(buf, value.Value!);
    }
}

//EventCodec registry id followed by the payload value
internal sealed class EventCodec : StreamCodec<RegistryFriendlyByteBuf, Event>
{
    public Event Decode(RegistryFriendlyByteBuf buf)
    {
        var subscription = DebugSubscriptionCodec.Read(buf);
        if (subscription.ValueCodec is null)
            throw new InvalidOperationException($"debug subscription {subscription} carries no value codec");
        return new Event(subscription, subscription.ValueCodec.Decode(buf));
    }

    public void Encode(RegistryFriendlyByteBuf buf, Event value)
    {
        DebugSubscriptionCodec.Write(buf, value.Subscription);
        if (value.Subscription.ValueCodec is null)
            throw new InvalidOperationException($"debug subscription {value.Subscription} carries no value codec");
        value.Subscription.ValueCodec.Encode(buf, value.Value);
    }
}

//DebugSubscriptionCodec registry id read/write shared by the update and event codecs
internal static class DebugSubscriptionCodec
{
    public static DebugSubscription Read(RegistryFriendlyByteBuf buf)
    {
        var id = buf.ReadVarInt();
        var registry = buf.Lookup(Registries.DEBUG_SUBSCRIPTION);
        return registry.ById(id) as DebugSubscription
            ?? throw new InvalidOperationException($"unknown debug subscription id {id}");
    }

    public static void Write(RegistryFriendlyByteBuf buf, DebugSubscription value)
    {
        var registry = buf.Lookup(Registries.DEBUG_SUBSCRIPTION);
        var id = registry.GetId(value);
        if (id == IdMap<object>.Default)
            throw new InvalidOperationException($"debug subscription not registered: {value}");
        buf.WriteVarInt(id);
    }
}
