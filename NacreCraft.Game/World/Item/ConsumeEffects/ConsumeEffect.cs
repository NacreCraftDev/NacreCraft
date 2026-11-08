using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.ConsumeEffects;

//ConsumeEffect an effect applied when an item is consumed, maps to vanilla net.minecraft.world.item.consume_effects.ConsumeEffect
//Vanilla dispatches on the registered type; NC keeps a concrete type object per effect in BuiltInRegistries.CONSUME_EFFECT_TYPE so the
//persistence and stream codecs can share the same registry-name dispatch
public abstract class ConsumeEffect
{
    //Codec persistence codec, dispatches on the type field and lets the type decode its own fields
    //Maps to vanilla BuiltInRegistries.CONSUME_EFFECT_TYPE.byNameCodec().dispatch(...)
    public static readonly Codec<ConsumeEffect> Codec = new ConsumeEffectCodec();

    //StreamCodec network codec, dispatches on the registry id of the type
    //Maps to vanilla ByteBufCodecs.registry(CONSUME_EFFECT_TYPE).dispatch(...)
    public static readonly StreamCodec<RegistryFriendlyByteBuf, ConsumeEffect> StreamCodec =
        ByteBufCodecs.RegistryValue<NacreCraft.Registry.ConsumeEffectType<object>>(Registries.CONSUME_EFFECT_TYPE)
            .Dispatch(effect => effect.Type, type => ((ConsumeEffectType)type).TypeStreamCodec);

    //Type the owning type object, used by both codecs and for registry comparison
    public abstract ConsumeEffectType Type { get; }

    //Apply runs the effect on the consumer, returns whether anything changed
    //Maps to vanilla apply(Level, ItemStack, LivingEntity); NC effects take the storage ServerLevel so they can play sounds and post game events
    public abstract bool Apply(ServerLevel level, ItemStack stack, LivingEntity user);
}

//ConsumeEffectType base type, maps to vanilla ConsumeEffect.Type
//Holds the registry id and the two codecs; concrete types register themselves in their static field
public abstract class ConsumeEffectType : NacreCraft.Registry.ConsumeEffectType<object>
{
    public Identifier Id { get; }

    protected ConsumeEffectType(Identifier id) => Id = id;

    //Decode reads the effect fields; the type field is already consumed by the dispatcher
    public abstract DataResult<ConsumeEffect> Decode<U>(DynamicOps<U> ops, MapLike<U> input);

    //EncodeFields writes the effect fields into an already started map; the dispatcher adds the type field
    public abstract RecordBuilder<U> EncodeFields<U>(DynamicOps<U> ops, ConsumeEffect value, RecordBuilder<U> builder);

    //TypeStreamCodec the type-specific network codec, surfaced as the generic effect so the dispatcher can chain it
    public abstract StreamCodec<RegistryFriendlyByteBuf, ConsumeEffect> TypeStreamCodec { get; }

    //Register registers a type into CONSUME_EFFECT_TYPE and returns it, so a static field can be assigned directly
    public static T Register<T>(Identifier id, T type) where T : ConsumeEffectType
    {
        Registry<NacreCraft.Registry.ConsumeEffectType<object>>.Register(
            BuiltInRegistries.CONSUME_EFFECT_TYPE, id, type);
        return type;
    }

    public override string ToString() => $"ConsumeEffectType[{Id}]";
}

//ConsumeEffectType<T> typed middle layer; concrete types only supply their codec pair
public abstract class ConsumeEffectType<T> : ConsumeEffectType where T : ConsumeEffect
{
    private readonly Codec<T> _codec;
    private readonly StreamCodec<RegistryFriendlyByteBuf, T> _streamCodec;

    protected ConsumeEffectType(Identifier id, Codec<T> codec, StreamCodec<RegistryFriendlyByteBuf, T> streamCodec)
        : base(id)
    {
        _codec = codec;
        _streamCodec = streamCodec;
    }

    public sealed override DataResult<ConsumeEffect> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
        => _codec.Decode(ops, input).Map(value => (ConsumeEffect)value);

    public sealed override RecordBuilder<U> EncodeFields<U>(DynamicOps<U> ops, ConsumeEffect value,
        RecordBuilder<U> builder)
    {
        if (value is T typed) _codec.EncodeTo(ops, typed, builder);
        return builder;
    }

    public sealed override StreamCodec<RegistryFriendlyByteBuf, ConsumeEffect> TypeStreamCodec
        => _streamCodec.Map(value => (ConsumeEffect)value, value => (T)value);
}

//SimpleConsumeEffectType concrete type holder for effects that only need a codec pair, maps to a registered ConsumeEffect.Type
//Each effect keeps one static instance; the generic base forwards decode, encode and stream dispatch to this pair
public sealed class SimpleConsumeEffectType<T> : ConsumeEffectType<T> where T : ConsumeEffect
{
    public SimpleConsumeEffectType(Identifier id, Codec<T> codec,
        StreamCodec<RegistryFriendlyByteBuf, T> streamCodec) : base(id, codec, streamCodec)
    {
    }
}

//ConsumeEffectCodec dispatch by the type field, then let the type decode the same map
//Maps to vanilla BuiltInRegistries.CONSUME_EFFECT_TYPE.byNameCodec().dispatch(...), with the type and its fields on one level
internal sealed class ConsumeEffectCodec : ScalarCodec<ConsumeEffect>
{
    public override DataResult<ConsumeEffect> Parse<U>(DynamicOps<U> ops, U input)
        => ops.GetMap(input).FlatMap(map => DecodeEffect(ops, map));

    private static DataResult<ConsumeEffect> DecodeEffect<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var typeTag = input.Get("type");
        if (!typeTag.IsPresent) return DataResult<ConsumeEffect>.Error(() => "consume effect is missing the type field");
        var typeText = ops.GetStringValue(typeTag.Get());
        if (!typeText.Result().IsPresent)
            return DataResult<ConsumeEffect>.Error(() => "consume effect type must be a string");
        var typeId = Identifier.TryParse(typeText.GetOrThrow());
        if (typeId is null)
            return DataResult<ConsumeEffect>.Error(() => $"invalid consume effect type: {typeText.GetOrThrow()}");
        if (BuiltInRegistries.CONSUME_EFFECT_TYPE.GetValue(typeId.Value) is not ConsumeEffectType type)
            return DataResult<ConsumeEffect>.Error(() => $"unknown consume effect type: {typeId}");
        return type.Decode(ops, input);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, ConsumeEffect value)
    {
        var builder = ops.MapBuilder();
        builder.Add("type", ops.CreateString(value.Type.Id.ToString()));
        value.Type.EncodeFields(ops, value, builder);
        return builder.Build(ops.Empty());
    }
}
