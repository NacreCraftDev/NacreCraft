using NacreCraft.Codec;
using NacreCraft.Registry;
using RegistrySpawnCondition = NacreCraft.Registry.SpawnCondition;

namespace NacreCraft.Game.World.Entity.Variant;

//BiomeCheck requires the spawn position to be in one of the listed biomes, maps to vanilla net.minecraft.world.entity.variant.BiomeCheck
public sealed record BiomeCheck(HolderSet<Biome> RequiredBiomes) : SpawnCondition
{
    public static readonly MapCodec<BiomeCheck> MAP_CODEC = RecordCodecBuilder.Of1(
        HolderSetCodecs.BiomeSet.FieldOf("biomes").ForGetter((BiomeCheck check) => check.RequiredBiomes),
        biomes => new BiomeCheck(biomes));

    public static readonly MapCodec<RegistrySpawnCondition> ConditionCodec =
        new XmapMapCodec<BiomeCheck, RegistrySpawnCondition>(MAP_CODEC, value => value, condition => (BiomeCheck)condition);

    public MapCodec<RegistrySpawnCondition> Codec() => ConditionCodec;

    public bool Test(SpawnContext context) => RequiredBiomes.Contains(context.Biome);
}

