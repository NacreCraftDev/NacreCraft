using NacreCraft.Primitives;

namespace NacreCraft.Registry.Environment;

//AttributeMapLayer applies one attribute map as a constant layer
//Vanilla builds this from map.get(attribute)::applyModifier; C# cannot close the generic over an unknown Value, so the layer stays untyped
public sealed class AttributeMapLayer(EnvironmentAttributeMap map, IEnvironmentAttribute attribute)
    : EnvironmentAttributeLayer.IConstantLayer
{
    public object Apply(object value, int cacheTickId, Vec3 position, SpatialAttributeInterpolator? biomeWeights,
        bool positional)
        => map.ApplyModifierObject(attribute, value);
}

//BiomeAttributeLayer reads the attribute from the biome at the sample position
//Vanilla adds one such layer per attribute any biome provides, maps to EnvironmentAttributeSystem.addBiomeLayerForAttribute
public sealed class BiomeAttributeLayer(
    IEnvironmentAttribute attribute, Func<Vec3, EnvironmentAttributeMap?> biomeAttributesAt)
    : EnvironmentAttributeLayer.IPositionalLayer
{
    public object Apply(object value, int cacheTickId, Vec3 position, SpatialAttributeInterpolator? biomeWeights,
        bool positional)
    {
        if (!positional) return value;
        if (biomeWeights is not null && attribute.IsSpatiallyInterpolated)
            return biomeWeights.ApplyAttributeLayerObject(attribute, value);
        return biomeAttributesAt(position)?.ApplyModifierObject(attribute, value) ?? value;
    }
}
