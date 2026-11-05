using NacreCraft.Codec;
using NacreCraft.Registry;
using RegistrySpawnCondition = NacreCraft.Registry.SpawnCondition;

namespace NacreCraft.Game.World.Entity.Variant;

//SpawnConditions registers the built-in spawn conditions, maps to vanilla net.minecraft.world.entity.variant.SpawnConditions
public static class SpawnConditions
{
    public static MapCodec<RegistrySpawnCondition> Bootstrap(Registry<MapCodec<RegistrySpawnCondition>> registry)
    {
        Registry<MapCodec<RegistrySpawnCondition>>.Register(registry,
            Identifier.WithDefaultNamespace("structure"), StructureCheck.ConditionCodec);
        Registry<MapCodec<RegistrySpawnCondition>>.Register(registry,
            Identifier.WithDefaultNamespace("moon_brightness"), MoonBrightnessCheck.ConditionCodec);
        return Registry<MapCodec<RegistrySpawnCondition>>.Register(registry,
            Identifier.WithDefaultNamespace("biome"), BiomeCheck.ConditionCodec);
    }
}

