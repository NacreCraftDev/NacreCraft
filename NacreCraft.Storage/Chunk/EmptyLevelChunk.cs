using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;

namespace NacreCraft.Storage;

//EmptyLevelChunk a chunk that is always empty, maps to vanilla net.minecraft.world.level.chunk.EmptyLevelChunk
//The void around the world uses it: reads answer air and writes are dropped, so nothing has to be generated
public sealed class EmptyLevelChunk : LevelChunk
{
    private readonly Holder<Biome> _biome;

    public EmptyLevelChunk(ChunkPos pos, int minSectionY, int sectionsCount,
        Func<Paletted.PalettedContainer<BlockState>> statesFactory,
        Func<Paletted.PalettedContainer<Holder<Biome>>> biomesFactory,
        Holder<Biome> biome, object? level = null)
        : base(pos, minSectionY, sectionsCount, statesFactory, biomesFactory, level)
        => _biome = biome;

    //Air state id 0 is the registry default, which is what the void reports
    public override BlockState GetBlockState(int worldX, int worldY, int worldZ) => default;

    public override void SetBlockState(int worldX, int worldY, int worldZ, BlockState state)
    {
    }

    public override Holder<Biome> GetNoiseBiome(int quartX, int quartY, int quartZ) => _biome;
}
