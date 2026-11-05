using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Environment;

namespace NacreCraft.Game.World.Entity.Variant;

//SpawnContext everything a spawn condition may read, maps to vanilla net.minecraft.world.entity.variant.SpawnContext
public sealed record SpawnContext(BlockPos Pos, ServerLevelAccessor Level, EnvironmentAttributeReader EnvironmentAttributes,
    Holder<Biome> Biome)
{
    //Create samples the biome and environment attributes at the position, maps to vanilla create
    public static SpawnContext Create(ServerLevelAccessor level, BlockPos pos)
        => new(pos, level, level.GetEnvironmentAttributes(),
            level.GetNoiseBiome(QuartPos.FromBlock(pos.X), QuartPos.FromBlock(pos.Y), QuartPos.FromBlock(pos.Z)));
}
