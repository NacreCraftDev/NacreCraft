namespace NacreCraft.Registry.Environment;

//SpatialAttributeInterpolator blends attribute values of the biomes around a sample point by weight
//maps to vanilla net.minecraft.world.attribute.SpatialAttributeInterpolator
//The source maps are keyed by reference and kept in insertion order, matching the fastutil Reference2DoubleArrayMap
public sealed class SpatialAttributeInterpolator
{
    private readonly List<KeyValuePair<EnvironmentAttributeMap, double>> _weightsBySource = new();

    public void Clear() => _weightsBySource.Clear();

    //Accumulate adds one source map's weight, summing the weight when the same map repeats
    public SpatialAttributeInterpolator Accumulate(double weight, EnvironmentAttributeMap attributes)
    {
        for (var i = 0; i < _weightsBySource.Count; i++)
        {
            if (!ReferenceEquals(_weightsBySource[i].Key, attributes)) continue;
            _weightsBySource[i] = new(attributes, _weightsBySource[i].Value + weight);
            return this;
        }
        _weightsBySource.Add(new(attributes, weight));
        return this;
    }

    //ApplyAttributeLayer applies the weighted sources to the base value, maps to vanilla applyAttributeLayer
    public Value ApplyAttributeLayer<Value>(EnvironmentAttribute<Value> attribute, Value baseValue)
        => (Value)ApplyAttributeLayerObject(attribute, baseValue!)!;

    //ApplyAttributeLayerObject untyped variant, used when only the weak-typed attribute is known
    public object ApplyAttributeLayerObject(IEnvironmentAttribute attribute, object baseValue)
    {
        if (_weightsBySource.Count == 0) return baseValue;
        if (_weightsBySource.Count == 1)
            return _weightsBySource[0].Key.ApplyModifierObject(attribute, baseValue);

        var lerp = attribute.SpatialLerpBoxed;
        var accumulatedWeight = 0.0;
        object? resultValue = null;
        foreach (var (sourceAttributes, sourceWeight) in _weightsBySource)
        {
            var sourceValue = sourceAttributes.ApplyModifierObject(attribute, baseValue);
            accumulatedWeight += sourceWeight;
            if (resultValue is null)
            {
                resultValue = sourceValue;
                continue;
            }
            var relativeFraction = (float)(sourceWeight / accumulatedWeight);
            resultValue = lerp.Apply(relativeFraction, resultValue, sourceValue);
        }
        return resultValue!;
    }
}
