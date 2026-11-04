using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Environment;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Attribute;

//EnvironmentAttributeProbe samples environment attributes around an entity for rendering, maps to vanilla net.minecraft.world.attribute.EnvironmentAttributeProbe
//Vanilla reads level.environmentAttributes(); this port's ServerLevel cannot reference the attribute system, so the reader is injected
public sealed class EnvironmentAttributeProbe
{
    private readonly EnvironmentAttributeReader _reader;
    private readonly Dictionary<IEnvironmentAttribute, ValueProbe> _valueProbes = new();
    private readonly SpatialAttributeInterpolator _biomeInterpolator = new();
    private ServerLevel? _level;
    private Vec3? _position;

    public EnvironmentAttributeProbe(EnvironmentAttributeReader? reader = null)
        => _reader = reader ?? EnvironmentAttributeReader.Empty;

    //Reset drops the level, position and every cached value, maps to vanilla reset
    public void Reset()
    {
        _level = null;
        _position = null;
        _biomeInterpolator.Clear();
        _valueProbes.Clear();
    }

    //Tick moves the probe and resamples the surrounding biome weights, maps to vanilla tick
    public void Tick(ServerLevel level, Vec3 position)
    {
        _level = level;
        _position = position;
        List<IEnvironmentAttribute>? stale = null;
        foreach (var (attribute, probe) in _valueProbes)
            if (probe.Tick()) (stale ??= new List<IEnvironmentAttribute>()).Add(attribute);
        if (stale is not null)
            foreach (var attribute in stale) _valueProbes.Remove(attribute);

        _biomeInterpolator.Clear();
        GaussianSampler.Sample(position.Multiply(0.25), (x, y, z) => NoiseBiomeAt(level, x, y, z),
            (weight, biome) =>
            {
                if (biome is not null) _biomeInterpolator.Accumulate(weight, biome.Attributes);
            });
    }

    //GetValue the value blended by the partial tick, maps to vanilla getValue
    public Value GetValue<Value>(EnvironmentAttribute<Value> attribute, float partialTicks)
    {
        if (!_valueProbes.TryGetValue(attribute, out var probe))
            _valueProbes[attribute] = probe = new ValueProbe(this, attribute);
        return (Value)probe.Get(partialTicks);
    }

    //NoiseBiomeAt reads the biome of a quart coordinate through the block coordinate it covers
    private static Biome? NoiseBiomeAt(ServerLevel level, int quartX, int quartY, int quartZ)
        => level.GetBiome(new BlockPos(quartX * 4, quartY * 4, quartZ * 4))?.Value;

    //ValueProbe one attribute's previous and current value, maps to vanilla ValueProbe
    private sealed class ValueProbe(EnvironmentAttributeProbe owner, IEnvironmentAttribute attribute)
    {
        private object? _lastValue;
        private object? _newValue;

        //Tick shifts the current value into the previous slot; a probe without a current value is dropped
        public bool Tick()
        {
            if (_newValue is null) return true;
            _lastValue = _newValue;
            _newValue = null;
            return false;
        }

        //Get recomputes the current value on first read and blends it by the partial tick
        public object Get(float partialTicks)
        {
            _newValue ??= owner.GetValueFromLevel(attribute);
            return attribute.PartialTickLerpBoxed.Apply(partialTicks, _lastValue!, _newValue!);
        }
    }

    //GetValueFromLevel reads the current value at the probe position, or the default when the probe is unset
    private object GetValueFromLevel(IEnvironmentAttribute attribute)
        => _level is null || _position is null
            ? attribute.DefaultValueObject
            : _reader.GetValueObject(attribute, _position.Value, _biomeInterpolator);
}
