using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage.Chunk;

namespace NacreCraft.Storage;

//ImposterProtoChunk a proto chunk view over a full chunk, maps to vanilla net.minecraft.world.level.chunk.ImposterProtoChunk
//Generation stages that only take a ProtoChunk get this wrapper, which forwards reads to the already loaded LevelChunk
//Writes are forwarded only when allowWrites is set, so a read-only stage cannot mutate the live chunk
public sealed class ImposterProtoChunk : ProtoChunk
{
    private readonly LevelChunk _wrapped;
    private readonly bool _allowWrites;

    public ImposterProtoChunk(LevelChunk wrapped, bool allowWrites,
        Func<Paletted.PalettedContainer<BlockState>> statesFactory,
        Func<Paletted.PalettedContainer<Holder<Biome>>> biomesFactory)
        : base(wrapped.Pos, wrapped.MinSectionY, wrapped.SectionsCount, statesFactory, biomesFactory)
    {
        _wrapped = wrapped;
        _allowWrites = allowWrites;
    }

    public LevelChunk Wrapped => _wrapped;

    public override BlockState GetBlockState(int worldX, int worldY, int worldZ)
        => _wrapped.GetBlockState(worldX, worldY, worldZ);

    public override void SetBlockState(int worldX, int worldY, int worldZ, BlockState state)
    {
        if (_allowWrites) _wrapped.SetBlockState(worldX, worldY, worldZ, state);
    }

    public override LevelChunkSection? GetSection(int sectionY)
        => _allowWrites ? _wrapped.GetSection(sectionY) : base.GetSection(sectionY);

    public override Holder<Biome> GetNoiseBiome(int quartX, int quartY, int quartZ)
        => _wrapped.GetNoiseBiome(quartX, quartY, quartZ);
}
