using NacreCraft.Primitives;

namespace NacreCraft.Registry.Environment;

//EnvironmentAttributeSampler bakes one attribute's layer chain and evaluates it, maps to vanilla EnvironmentAttributeSystem.ValueSampler
//The layer chain is driven through EnvironmentAttributeLayer.Layer, so the sampler itself stays untyped
public sealed class EnvironmentAttributeSampler
{
    private readonly IEnvironmentAttribute _attribute;
    private readonly object _baseValue;
    private readonly IReadOnlyList<EnvironmentAttributeLayer.Layer> _layers;
    private readonly bool _isAffectedByPosition;
    private object? _cachedTickValue;
    private int _cacheTickId;

    private EnvironmentAttributeSampler(IEnvironmentAttribute attribute, object baseValue,
        IReadOnlyList<EnvironmentAttributeLayer.Layer> layers, bool isAffectedByPosition)
    {
        _attribute = attribute;
        _baseValue = baseValue;
        _layers = layers;
        _isAffectedByPosition = isAffectedByPosition;
    }

    //Bake folds the leading constant layers into the base value, maps to vanilla bakeLayerSampler
    public static EnvironmentAttributeSampler Bake(IEnvironmentAttribute attribute, IEnumerable<object> untypedLayers)
    {
        var layers = untypedLayers.ToList();
        var baseValue = attribute.DefaultValueObject;
        while (layers.Count > 0 && layers[0] is EnvironmentAttributeLayer.IConstantLayer)
        {
            baseValue = ((EnvironmentAttributeLayer.Layer)layers[0]).Apply(baseValue, 0, default, null, false);
            layers.RemoveAt(0);
        }
        var typed = layers.Cast<EnvironmentAttributeLayer.Layer>().ToList();
        return new EnvironmentAttributeSampler(attribute, baseValue, typed, typed.Any(layer => layer is EnvironmentAttributeLayer.IPositionalLayer));
    }

    //BaseValue the folded constant base value
    public object BaseValue => _baseValue;

    //IsAffectedByPosition whether any remaining layer depends on the sampling position
    public bool IsAffectedByPosition => _isAffectedByPosition;

    //InvalidateTickCache drops the per-tick cache, maps to vanilla invalidateTickCache
    public void InvalidateTickCache()
    {
        _cachedTickValue = null;
        _cacheTickId++;
    }

    //GetDimensionValue the value without a position; positional layers keep the value unchanged
    public Value GetDimensionValue<Value>()
    {
        if (_cachedTickValue is not null) return (Value)_cachedTickValue;
        var result = Compute(default, null, false);
        _cachedTickValue = result;
        return (Value)result;
    }

    //GetValue the value at a position, maps to vanilla ValueSampler.getValue
    public Value GetValue<Value>(Vec3 position, SpatialAttributeInterpolator? biomeWeights)
        => _isAffectedByPosition ? (Value)Compute(position, biomeWeights, true) : GetDimensionValue<Value>();

    private object Compute(Vec3 position, SpatialAttributeInterpolator? biomeWeights, bool positional)
    {
        var result = _baseValue;
        foreach (var layer in _layers)
            result = layer.Apply(result, _cacheTickId, position, biomeWeights, positional);
        return _attribute.SanitizeValueObject(result);
    }
}
