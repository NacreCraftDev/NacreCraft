using NacreCraft.Codec;
using NacreCraft.Game.World.Items.Enchanting;
using NacreCraft.Game.World.Particle;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;
using NacreCraft.Registry.Environment;
using NacreCraft.Util;
using AmbientSoundSettings = NacreCraft.Registry.Environment.AmbientSounds;
using BackgroundMusicSettings = NacreCraft.Registry.Environment.BackgroundMusic;

namespace NacreCraft.Game.World.Attribute;

//AttributeTypes built-in attribute types, maps to vanilla net.minecraft.world.attribute.AttributeTypes
//Registration order follows the vanilla field declaration order
public static class AttributeTypes
{
    public static readonly AttributeType<bool> Boolean = Register("boolean",
        AttributeType<bool>.OfNotInterpolated(Codecs.Bool, AttributeModifier.BooleanLibrary));

    public static readonly AttributeType<TriState> TriStateType = Register("tri_state",
        AttributeType<TriState>.OfNotInterpolated(TriStates.Codec));

    public static readonly AttributeType<float> Float = Register("float",
        AttributeType<float>.OfInterpolated(Codecs.Float, AttributeModifier.FloatLibrary,
            LerpFunction<float>.OfFloat(), LerpFunction<float>.OfFloat(), value => value));

    public static readonly AttributeType<float> AngleDegrees = Register("angle_degrees",
        AttributeType<float>.OfInterpolated(Codecs.Float, AttributeModifier.FloatLibrary,
            LerpFunction<float>.OfFloat(), LerpFunction<float>.OfDegrees(90.0f), value => value));

    public static readonly AttributeType<int> RgbColor = Register("rgb_color",
        AttributeType<int>.OfInterpolated(HexColorCodec.StringRgb, AttributeModifier.RgbColorLibrary,
            LerpFunction<int>.OfColor()));

    public static readonly AttributeType<int> ArgbColor = Register("argb_color",
        AttributeType<int>.OfInterpolated(HexColorCodec.StringArgb, AttributeModifier.ArgbColorLibrary,
            LerpFunction<int>.OfColor()));

    public static readonly AttributeType<int> Integer = Register("integer",
        AttributeType<int>.OfInterpolated(Codecs.Int, AttributeModifier.IntegerLibrary,
            LerpFunction<int>.OfInteger(), LerpFunction<int>.OfInteger(), value => value));

    public static readonly AttributeType<MoonPhase> MoonPhaseType = Register("moon_phase",
        AttributeType<MoonPhase>.OfNotInterpolated(MoonPhases.Codec));

    public static readonly AttributeType<Activity> ActivityType = Register("activity",
        AttributeType<Activity>.OfNotInterpolated(new RegistryNameCodec<Activity>(BuiltInRegistries.ACTIVITY)));

    public static readonly AttributeType<BedRule> BedRuleType = Register("bed_rule",
        AttributeType<BedRule>.OfNotInterpolated(NacreCraft.Game.World.Attribute.BedRule.Codec));

    public static readonly AttributeType<ParticleOptions> Particle = Register("particle",
        AttributeType<ParticleOptions>.OfNotInterpolated(ParticleOptionsCodec.Instance));

    public static readonly AttributeType<IReadOnlyList<AmbientParticle>> AmbientParticles = Register("ambient_particles",
        AttributeType<IReadOnlyList<AmbientParticle>>.OfNotInterpolated(AmbientParticle.Codec.ListOf()));

    public static readonly AttributeType<BackgroundMusicSettings> BackgroundMusicType = Register("background_music",
        AttributeType<BackgroundMusicSettings>.OfNotInterpolated(BackgroundMusicSettings.Codec));

    public static readonly AttributeType<AmbientSoundSettings> AmbientSoundsType = Register("ambient_sounds",
        AttributeType<AmbientSoundSettings>.OfNotInterpolated(AmbientSoundSettings.Codec));

    //Codec resolves an attribute type by registry name, maps to vanilla AttributeTypes.CODEC
    public static readonly Codec<IAttributeType> Codec = new RegistryNameCodec<IAttributeType>(BuiltInRegistries.ATTRIBUTE_TYPE);

    private static bool _registered;

    static AttributeTypes() { RegisterAll(); }

    //RegisterAll registers all built-in attribute types, idempotent on repeat calls
    public static void RegisterAll()
    {
        //The static field initializers already registered everything; touching the class runs them
        if (_registered) return;
        _registered = true;
    }

    private static AttributeType<Value> Register<Value>(string name, AttributeType<Value> type)
    {
        var id = Identifier.WithDefaultNamespace(name);
        if (!BuiltInRegistries.ATTRIBUTE_TYPE.ContainsKey(id))
            Registry<IAttributeType>.Register(BuiltInRegistries.ATTRIBUTE_TYPE, id, type);
        return type;
    }
}

//RegistryNameCodec resolves a registry element by name and writes the name back, maps to vanilla byNameCodec
internal sealed class RegistryNameCodec<T> : ScalarCodec<T> where T : class
{
    private readonly Registry<T> _registry;

    public RegistryNameCodec(Registry<T> registry) { _registry = registry; }

    public override DataResult<T> Parse<U>(DynamicOps<U> ops, U input)
        => IdentifierCodec.Instance.Parse(ops, input).FlatMap(id =>
        {
            var value = _registry.GetValue(id);
            return value is null
                ? DataResult<T>.Error(() => $"Unknown id in {_registry.Key}: {id}")
                : DataResult<T>.Success(value);
        });

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, T value)
    {
        var id = _registry.GetKey(value);
        return id is null
            ? DataResult<U>.Error(() => $"Element is not registered in {_registry.Key}")
            : DataResult<U>.Success(ops.CreateString(id.Value.ToString()));
    }
}
