using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen;

//BiomeResolver a source that resolves the biome of a quart position, maps to vanilla net.minecraft.world.level.biome.BiomeResolver
//BiomeSource is the full source interface; this narrower one is what chunk generation needs
public interface BiomeResolver
{
    Holder<Biome> GetNoiseBiome(int quartX, int quartY, int quartZ, Climate.Sampler sampler);
}
