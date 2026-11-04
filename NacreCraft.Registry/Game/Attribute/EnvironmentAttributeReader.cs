using NacreCraft.Primitives;

namespace NacreCraft.Registry.Environment;

//EnvironmentAttributeReader read access to environment attribute values, maps to vanilla EnvironmentAttributeReader
public interface EnvironmentAttributeReader
{
    //Empty a reader that always answers the attribute's default value, maps to vanilla EMPTY
    static EnvironmentAttributeReader Empty => EmptyEnvironmentAttributeReader.Instance;

    //GetDimensionValue the value without a position; positional attributes must not be read this way
    Value GetDimensionValue<Value>(EnvironmentAttribute<Value> attribute);

    //GetValue the value at a position, optionally using the biome weights sampled around it
    Value GetValue<Value>(EnvironmentAttribute<Value> attribute, Vec3 position,
        SpatialAttributeInterpolator? biomeWeights);

    //GetValueObject the value at a position in erased object form, for callers that only know the weak-typed attribute
    object GetValueObject(IEnvironmentAttribute attribute, Vec3 position, SpatialAttributeInterpolator? biomeWeights);
}

//EnvironmentAttributeReaderExtensions position overloads, maps to the default methods of the vanilla interface
public static class EnvironmentAttributeReaderExtensions
{
    //GetValue at a block position samples the block centre
    public static Value GetValue<Value>(this EnvironmentAttributeReader reader, EnvironmentAttribute<Value> attribute,
        BlockPos position)
        => reader.GetValue(attribute, new Vec3(position.X + 0.5, position.Y + 0.5, position.Z + 0.5));

    //GetValue at a vector position without biome weights
    public static Value GetValue<Value>(this EnvironmentAttributeReader reader, EnvironmentAttribute<Value> attribute,
        Vec3 position)
        => reader.GetValue(attribute, position, null);
}

//EmptyEnvironmentAttributeReader answers default values only
internal sealed class EmptyEnvironmentAttributeReader : EnvironmentAttributeReader
{
    public static readonly EmptyEnvironmentAttributeReader Instance = new();

    public Value GetDimensionValue<Value>(EnvironmentAttribute<Value> attribute) => attribute.DefaultValue;

    public Value GetValue<Value>(EnvironmentAttribute<Value> attribute, Vec3 position,
        SpatialAttributeInterpolator? biomeWeights)
        => attribute.DefaultValue;

    public object GetValueObject(IEnvironmentAttribute attribute, Vec3 position,
        SpatialAttributeInterpolator? biomeWeights)
        => attribute.DefaultValueObject;
}
