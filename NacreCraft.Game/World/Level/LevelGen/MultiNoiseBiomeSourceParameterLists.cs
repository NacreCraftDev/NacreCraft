using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen;

//MultiNoiseBiomeSourceParameterLists built-in multi-noise parameter list keys, maps to vanilla net.minecraft.world.level.biome.MultiNoiseBiomeSourceParameterLists
//NC normally loads these from worldgen/multi_noise_biome_source_parameter_list; Bootstrap is for callers that want them without a data pack
public static class MultiNoiseBiomeSourceParameterLists
{
    //NC keeps this registry object keyed, so the keys are object typed like the registry itself
    public static readonly ResourceKey<object> Nether = Key("nether");

    public static readonly ResourceKey<object> Overworld = Key("overworld");

    //Bootstrap registers the two built-in lists, skipping any key the data pack already provided
    public static void Bootstrap()
    {
        var registry = BuiltInRegistries.MULTI_NOISE_BIOME_SOURCE_PARAMETER_LIST;
        RegisterPreset(registry, Nether, MultiNoisePreset.Nether);
        RegisterPreset(registry, Overworld, MultiNoisePreset.Overworld);
    }

    private static void RegisterPreset(Registry<object> registry, ResourceKey<object> key, MultiNoisePreset preset)
    {
        if (registry.ContainsKey(key.Identifier)) return;
        var entries = preset.CreateEntries(biomeKey => BuiltInRegistries.BIOME.GetOrCreateHolder(biomeKey.Identifier));
        Registry<object>.Register(registry, key.Identifier, new MultiNoiseBiomeSourceParameterList(preset, entries));
    }

    private static ResourceKey<object> Key(string name)
        => ResourceKey<object>.Create(Registries.MULTI_NOISE_BIOME_SOURCE_PARAMETER_LIST,
            Identifier.WithDefaultNamespace(name));
}
