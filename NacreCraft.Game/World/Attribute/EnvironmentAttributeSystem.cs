using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Environment;
using NacreCraft.Storage;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Attribute;

//EnvironmentAttributeSystem layered environment attribute sampling, maps to vanilla net.minecraft.world.attribute.EnvironmentAttributeSystem
//Layers are collected per attribute and baked into one sampler; the per-tick cache is dropped by InvalidateTickCache
public sealed class EnvironmentAttributeSystem : EnvironmentAttributeReader
{
    private readonly Dictionary<IEnvironmentAttribute, EnvironmentAttributeSampler> _attributeSamplers = new();

    private EnvironmentAttributeSystem(Dictionary<IEnvironmentAttribute, List<object>> layersByAttribute)
    {
        foreach (var (attribute, layers) in layersByAttribute)
            _attributeSamplers[attribute] = EnvironmentAttributeSampler.Bake(attribute, layers);
    }

    public static Builder CreateBuilder() => new();

    //InvalidateTickCache drops every sampler's per-tick cache, maps to vanilla invalidateTickCache
    public void InvalidateTickCache()
    {
        foreach (var sampler in _attributeSamplers.Values)
            sampler.InvalidateTickCache();
    }

    public Value GetDimensionValue<Value>(EnvironmentAttribute<Value> attribute)
        => _attributeSamplers.TryGetValue(attribute, out var sampler)
            ? sampler.GetDimensionValue<Value>()
            : attribute.DefaultValue;

    public Value GetValue<Value>(EnvironmentAttribute<Value> attribute, Vec3 position,
        SpatialAttributeInterpolator? biomeWeights)
        => _attributeSamplers.TryGetValue(attribute, out var sampler)
            ? sampler.GetValue<Value>(position, biomeWeights)
            : attribute.DefaultValue;

    public object GetValueObject(IEnvironmentAttribute attribute, Vec3 position,
        SpatialAttributeInterpolator? biomeWeights)
        => _attributeSamplers.TryGetValue(attribute, out var sampler)
            ? sampler.GetValue<object>(position, biomeWeights)
            : attribute.DefaultValueObject;

    //AddDefaultLayers registers the dimension, biome, timeline and weather layers of a level, maps to vanilla addDefaultLayers
    private static void AddDefaultLayers(Builder builder, ServerLevel level)
    {
        //The dimension attribute layer is not registered: this port's DimensionType does not carry an attributes field yet
        AddBiomeLayers(builder, level);
        //Timeline layers are not registered for the same reason: DimensionType carries no timelines yet
        if (level.CanHaveWeather())
            WeatherAttributes.AddBuiltinLayers(builder, WeatherAttributes.WeatherAccess.From(level));
    }

    //AddBiomeLayers registers one positional layer per attribute any biome provides
    private static void AddBiomeLayers(Builder builder, ServerLevel level)
    {
        var seen = new HashSet<IEnvironmentAttribute>();
        foreach (var entry in BuiltInRegistries.BIOME.EntrySet)
        foreach (var attribute in entry.Value.Attributes.KeySet)
            if (seen.Add(attribute))
                builder.AddLayer(attribute, new BiomeAttributeLayer(attribute, position => BiomeAttributesAt(level, position)));
    }

    //BiomeAttributesAt reads the biome attributes at a position; an unloaded chunk gives null so the layer keeps the value
    private static EnvironmentAttributeMap? BiomeAttributesAt(ServerLevel level, Vec3 position)
        => level.GetBiome(new BlockPos(Mth.Floor(position.X), Mth.Floor(position.Y), Mth.Floor(position.Z)))?.Value.Attributes;

    //Builder collects layers per attribute, maps to vanilla EnvironmentAttributeSystem.Builder
    public sealed class Builder
    {
        private readonly Dictionary<IEnvironmentAttribute, List<object>> _layersByAttribute = new();

        internal Builder() { }

        public Builder AddDefaultLayers(ServerLevel level)
        {
            EnvironmentAttributeSystem.AddDefaultLayers(this, level);
            return this;
        }

        //AddConstantLayer adds every entry of the map as a constant layer
        public Builder AddConstantLayer(EnvironmentAttributeMap attributeMap)
        {
            foreach (var attribute in attributeMap.KeySet)
                AddLayer(attribute, new AttributeMapLayer(attributeMap, attribute));
            return this;
        }

        public Builder AddConstantLayer<Value>(EnvironmentAttribute<Value> attribute,
            EnvironmentAttributeLayer.Constant<Value> layer)
            => AddLayer(attribute, layer);

        public Builder AddTimeBasedLayer<Value>(EnvironmentAttribute<Value> attribute,
            EnvironmentAttributeLayer.TimeBased<Value> layer)
            => AddLayer(attribute, layer);

        public Builder AddPositionalLayer<Value>(EnvironmentAttribute<Value> attribute,
            EnvironmentAttributeLayer.Positional<Value> layer)
            => AddLayer(attribute, layer);

        //AddTimelineLayer adds one time-based layer per track the timeline carries
        public Builder AddTimelineLayer(Holder<NacreCraft.Registry.Timeline> timeline, ClockManager clockManager)
        {
            foreach (var raw in timeline.Value.Attributes)
                if (raw is IEnvironmentAttribute attribute
                    && attribute.CreateTimelineLayer(timeline.Value, clockManager) is { } layer)
                    AddLayer(attribute, layer);
            return this;
        }

        //AddLayer adds a layer whose value type is not known; callers holding only EnvironmentAttribute<?> use it
        public Builder AddLayer(IEnvironmentAttribute attribute, object layer)
        {
            if (!_layersByAttribute.TryGetValue(attribute, out var layers))
                _layersByAttribute[attribute] = layers = new List<object>();
            layers.Add(layer);
            return this;
        }

        public EnvironmentAttributeSystem Build() => new(_layersByAttribute);
    }
}
