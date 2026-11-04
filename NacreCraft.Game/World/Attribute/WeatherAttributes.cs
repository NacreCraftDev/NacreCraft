using NacreCraft.Game.World.Timeline;
using NacreCraft.Registry.Environment;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Attribute;

//WeatherAttributes the attribute maps rain and thunder apply, maps to vanilla net.minecraft.world.attribute.WeatherAttributes
public static class WeatherAttributes
{
    //Rain attribute changes while it rains
    public static readonly EnvironmentAttributeMap Rain = EnvironmentAttributeMap.CreateBuilder()
        .Modify(EnvironmentAttributes.SkyColor, RgbModifier(AttributeModifier.OperationId.BlendToGray),
            new BlendToGray(0.6f, 0.75f))
        .Modify(EnvironmentAttributes.FogColor, RgbModifier(AttributeModifier.OperationId.Multiply),
            Argb.ColorFromFloat(1.0f, 0.5f, 0.5f, 0.6f))
        .Modify(EnvironmentAttributes.CloudColor, ArgbModifier(AttributeModifier.OperationId.BlendToGray),
            new BlendToGray(0.24f, 0.5f))
        .Modify(EnvironmentAttributes.SkyLightLevel, FloatModifier(AttributeModifier.OperationId.AlphaBlend),
            new FloatWithAlpha(4.0f, 0.3125f))
        .Modify(EnvironmentAttributes.SkyLightColor, RgbModifier(AttributeModifier.OperationId.AlphaBlend),
            Argb.Color(0.3125f, Timelines.NightSkyLightColor))
        .Modify(EnvironmentAttributes.SkyLightFactor, FloatModifier(AttributeModifier.OperationId.AlphaBlend),
            new FloatWithAlpha(0.24f, 0.3125f))
        .Set(EnvironmentAttributes.StarBrightness, 0.0f)
        .Modify(EnvironmentAttributes.SunriseSunsetColor, ArgbModifier(AttributeModifier.OperationId.Multiply),
            Argb.ColorFromFloat(1.0f, 0.5f, 0.5f, 0.6f))
        .Set(EnvironmentAttributes.BeesStayInHive, true)
        .Build();

    //Thunder attribute changes while thunder rolls
    public static readonly EnvironmentAttributeMap Thunder = EnvironmentAttributeMap.CreateBuilder()
        .Modify(EnvironmentAttributes.SkyColor, RgbModifier(AttributeModifier.OperationId.BlendToGray),
            new BlendToGray(0.24f, 0.94f))
        .Modify(EnvironmentAttributes.FogColor, RgbModifier(AttributeModifier.OperationId.Multiply),
            Argb.ColorFromFloat(1.0f, 0.25f, 0.25f, 0.3f))
        .Modify(EnvironmentAttributes.CloudColor, ArgbModifier(AttributeModifier.OperationId.BlendToGray),
            new BlendToGray(0.095f, 0.94f))
        .Modify(EnvironmentAttributes.SkyLightLevel, FloatModifier(AttributeModifier.OperationId.AlphaBlend),
            new FloatWithAlpha(4.0f, 0.52734375f))
        .Modify(EnvironmentAttributes.SkyLightColor, RgbModifier(AttributeModifier.OperationId.AlphaBlend),
            Argb.Color(0.52734375f, Timelines.NightSkyLightColor))
        .Modify(EnvironmentAttributes.SkyLightFactor, FloatModifier(AttributeModifier.OperationId.AlphaBlend),
            new FloatWithAlpha(0.24f, 0.52734375f))
        .Set(EnvironmentAttributes.StarBrightness, 0.0f)
        .Modify(EnvironmentAttributes.SunriseSunsetColor, ArgbModifier(AttributeModifier.OperationId.Multiply),
            Argb.ColorFromFloat(1.0f, 0.25f, 0.25f, 0.3f))
        .Set(EnvironmentAttributes.BeesStayInHive, true)
        .Build();

    //WeatherAttributeSet union of the attributes the two maps touch
    private static readonly IReadOnlyList<IEnvironmentAttribute> WeatherAttributeSet = Union(Rain, Thunder);

    //AddBuiltinLayers registers one time-based layer per weather attribute, maps to vanilla addBuiltinLayers
    public static void AddBuiltinLayers(EnvironmentAttributeSystem.Builder system, WeatherAccess weatherAccess)
    {
        foreach (var attribute in WeatherAttributeSet)
            system.AddLayer(attribute, new WeatherLayer(weatherAccess, Rain.GetEntry(attribute),
                Thunder.GetEntry(attribute), attribute.StateChangeLerpBoxed));
    }

    private static AttributeModifier<int, object> RgbModifier(AttributeModifier.OperationId id)
        => AttributeModifier.RgbColorLibrary[id];

    private static AttributeModifier<int, object> ArgbModifier(AttributeModifier.OperationId id)
        => AttributeModifier.ArgbColorLibrary[id];

    private static AttributeModifier<float, object> FloatModifier(AttributeModifier.OperationId id)
        => AttributeModifier.FloatLibrary[id];

    private static IReadOnlyList<IEnvironmentAttribute> Union(EnvironmentAttributeMap rain, EnvironmentAttributeMap thunder)
    {
        var attributes = new List<IEnvironmentAttribute>();
        foreach (var attribute in rain.KeySet)
            if (!attributes.Contains(attribute)) attributes.Add(attribute);
        foreach (var attribute in thunder.KeySet)
            if (!attributes.Contains(attribute)) attributes.Add(attribute);
        return attributes;
    }

    //WeatherLayer blends the rain and thunder entries over the base value by their current levels
    private sealed class WeatherLayer(
        WeatherAccess weatherAccess,
        EnvironmentAttributeMap.Entry? rainEntry,
        EnvironmentAttributeMap.Entry? thunderEntry,
        LerpFunction<object> stateChangeLerp) : EnvironmentAttributeLayer.TimeBased<object>
    {
        public object ApplyTimeBased(object value, int cacheTickId)
        {
            var thunderLevel = weatherAccess.ThunderLevel;
            var rainLevel = weatherAccess.RainLevel - thunderLevel;
            if (rainEntry is not null && rainLevel > 0.0f)
                value = stateChangeLerp.Apply(rainLevel, value, rainEntry.ApplyModifierObject(value));
            if (thunderEntry is not null && thunderLevel > 0.0f)
                value = stateChangeLerp.Apply(thunderLevel, value, thunderEntry.ApplyModifierObject(value));
            return value;
        }
    }

    //WeatherAccess weather levels of a level, maps to vanilla WeatherAttributes.WeatherAccess
    public interface WeatherAccess
    {
        float RainLevel { get; }

        float ThunderLevel { get; }

        //From reads the levels of a level, maps to vanilla WeatherAccess.from
        static WeatherAccess From(ServerLevel level) => new LevelWeatherAccess(level);
    }

    //LevelWeatherAccess reads the interpolated levels of a level
    private sealed class LevelWeatherAccess(ServerLevel level) : WeatherAccess
    {
        public float RainLevel => level.GetRainLevel(1.0f);

        public float ThunderLevel => level.GetThunderLevel(1.0f);
    }
}
