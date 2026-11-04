using NacreCraft.Codec;

namespace NacreCraft.Registry.Environment;

//IAttributeType weak-typed view of an attribute type, so attribute types of different value types can share one registry
public interface IAttributeType
{
}

//AttributeType environment attribute type describing the value codec and allowed modifiers, maps to vanilla AttributeType
public sealed class AttributeType<Value> : IAttributeType
{
    private static readonly IReadOnlyDictionary<AttributeModifier.OperationId, AttributeModifier<Value, object>> EmptyLibrary =
        new Dictionary<AttributeModifier.OperationId, AttributeModifier<Value, object>>();

    public Codec<Value> ValueCodec { get; }

    public IReadOnlyDictionary<AttributeModifier.OperationId, AttributeModifier<Value, object>> ModifierLibrary { get; }

    public Codec<AttributeModifier<Value, object>> ModifierCodec { get; }

    //KeyframeLerp how two keyframe arguments of this type blend, maps to vanilla AttributeType.keyframeLerp
    public LerpFunction<Value> KeyframeLerp { get; }

    //StateChangeLerp how a discrete state change blends, maps to vanilla AttributeType.stateChangeLerp
    public LerpFunction<Value> StateChangeLerp { get; }

    //SpatialLerp how neighbouring biome values blend by weight, maps to vanilla AttributeType.spatialLerp
    public LerpFunction<Value> SpatialLerp { get; }

    //PartialTickLerp how two ticks blend for rendering, maps to vanilla AttributeType.partialTickLerp
    public LerpFunction<Value> PartialTickLerp { get; }

    //ToFloatFunction optional float projection, null when the value cannot be represented as a float
    public Func<Value, float>? ToFloatFunction { get; }

    //SpatialLerpBoxed the spatial lerp adapted to the erased object form untyped callers use
    public LerpFunction<object> SpatialLerpBoxed { get; }

    //StateChangeLerpBoxed the state-change lerp adapted to the erased object form untyped callers use
    public LerpFunction<object> StateChangeLerpBoxed { get; }

    //PartialTickLerpBoxed the partial-tick lerp adapted to the erased object form untyped callers use
    public LerpFunction<object> PartialTickLerpBoxed { get; }

    private AttributeType(Codec<Value> valueCodec,
        IReadOnlyDictionary<AttributeModifier.OperationId, AttributeModifier<Value, object>> modifierLibrary,
        LerpFunction<Value> keyframeLerp,
        LerpFunction<Value> stateChangeLerp,
        LerpFunction<Value> spatialLerp,
        LerpFunction<Value> partialTickLerp,
        Func<Value, float>? toFloatFunction)
    {
        ValueCodec = valueCodec;
        ModifierLibrary = modifierLibrary;
        KeyframeLerp = keyframeLerp;
        StateChangeLerp = stateChangeLerp;
        SpatialLerp = spatialLerp;
        PartialTickLerp = partialTickLerp;
        ToFloatFunction = toFloatFunction;
        SpatialLerpBoxed = new BoxedLerp<Value>(spatialLerp);
        StateChangeLerpBoxed = new BoxedLerp<Value>(stateChangeLerp);
        PartialTickLerpBoxed = new BoxedLerp<Value>(partialTickLerp);
        ModifierCodec = new ModifierCodecImpl(this);
    }

    //OfInterpolated interpolatable value type that uses the same lerp everywhere
    public static AttributeType<Value> OfInterpolated(Codec<Value> valueCodec,
        IReadOnlyDictionary<AttributeModifier.OperationId, AttributeModifier<Value, object>> modifierLibrary,
        LerpFunction<Value> lerp)
        => OfInterpolated(valueCodec, modifierLibrary, lerp, lerp, null);

    //OfInterpolated with a separate partial-tick lerp and an optional float projection
    public static AttributeType<Value> OfInterpolated(Codec<Value> valueCodec,
        IReadOnlyDictionary<AttributeModifier.OperationId, AttributeModifier<Value, object>> modifierLibrary,
        LerpFunction<Value> lerp,
        LerpFunction<Value> partialTickLerp,
        Func<Value, float>? toFloatFunction)
        => new(valueCodec, modifierLibrary, lerp, lerp, lerp, partialTickLerp, toFloatFunction);

    //OfNotInterpolated non-interpolatable value type; with no library only override is allowed
    //The three steps match vanilla: keyframe at the segment end, state change immediately, spatial at the midpoint
    public static AttributeType<Value> OfNotInterpolated(Codec<Value> valueCodec,
        IReadOnlyDictionary<AttributeModifier.OperationId, AttributeModifier<Value, object>>? modifierLibrary = null)
        => new(valueCodec, modifierLibrary ?? EmptyLibrary,
            LerpFunction<Value>.OfStep(1.0f), LerpFunction<Value>.OfStep(0.0f),
            LerpFunction<Value>.OfStep(0.5f), LerpFunction<Value>.OfStep(0.0f), null);

    //ToFloat projects the value to a float; throws when the type has no projection, maps to vanilla toFloat
    public float ToFloat(Value value)
    {
        if (ToFloatFunction is null)
            throw new InvalidOperationException($"{value} cannot be represented as a float");
        return ToFloatFunction(value);
    }

    //CheckAllowedModifier checks whether the modifier belongs to this type
    public void CheckAllowedModifier(AttributeModifier<Value, object> modifier)
    {
        if (ReferenceEquals(modifier, AttributeModifier.Override<Value>())) return;
        if (ModifierLibrary.Values.Any(existing => ReferenceEquals(existing, modifier))) return;
        throw new ArgumentException($"Modifier {modifier} is not valid for {this}");
    }

    //GetOperationId reverse-looks up the modifier's operation id, returning null if not found
    public AttributeModifier.OperationId? GetOperationId(AttributeModifier<Value, object> modifier)
    {
        if (ReferenceEquals(modifier, AttributeModifier.Override<Value>())) return AttributeModifier.OperationId.Override;
        foreach (var (id, existing) in ModifierLibrary)
            if (ReferenceEquals(existing, modifier)) return id;
        return null;
    }

    //ToString uses the registry name, maps to vanilla AttributeType.toString
    public override string ToString()
    {
        var id = BuiltInRegistries.ATTRIBUTE_TYPE.GetKey(this);
        return id is null ? "unregistered" : id.Value.ToString();
    }

    //ModifierCodecImpl resolves a modifier by operation id name; override is always available and the rest must be in the type's library
    private sealed class ModifierCodecImpl : ScalarCodec<AttributeModifier<Value, object>>
    {
        private readonly AttributeType<Value> _type;

        public ModifierCodecImpl(AttributeType<Value> type) { _type = type; }

        public override DataResult<AttributeModifier<Value, object>> Parse<U>(DynamicOps<U> ops, U input)
            => AttributeModifier.OperationIdCodec.Parse(ops, input).FlatMap(id =>
            {
                if (id == AttributeModifier.OperationId.Override)
                    return DataResult<AttributeModifier<Value, object>>.Success(AttributeModifier.Override<Value>());
                return _type.ModifierLibrary.TryGetValue(id, out var modifier)
                    ? DataResult<AttributeModifier<Value, object>>.Success(modifier)
                    : DataResult<AttributeModifier<Value, object>>.Error(() => $"Modifier {id} is not valid for this attribute type");
            });

        public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, AttributeModifier<Value, object> value)
        {
            var id = _type.GetOperationId(value);
            return id is null
                ? DataResult<U>.Error(() => "Unknown modifier")
                : AttributeModifier.OperationIdCodec.EncodeStart(ops, id.Value);
        }
    }
}
