using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage.Chunk;

namespace NacreCraft.Storage;

//BulkSectionAccess fast read access over the sections of one chunk, maps to vanilla net.minecraft.world.level.chunk.BulkSectionAccess
//Caches the last section so a neighbour scan does not repeat the section lookup per position
//Vanilla pins sections with acquire/release; NC sections have no lifecycle, so dispose is a no-op
public sealed class BulkSectionAccess : IDisposable
{
    private readonly ChunkAccess _chunk;
    private LevelChunkSection? _lastSection;
    private int _lastSectionY = int.MinValue;

    public BulkSectionAccess(ChunkAccess chunk) => _chunk = chunk;

    //GetSection the section containing the given position, null when outside the chunk height
    public LevelChunkSection? GetSection(BlockPos pos)
    {
        var sectionY = pos.Y >> 4;
        if (sectionY < _chunk.MinSectionY || sectionY > _chunk.MaxSectionY) return null;
        if (_lastSection is null || _lastSectionY != sectionY)
        {
            _lastSection = _chunk.GetSection(sectionY);
            _lastSectionY = sectionY;
        }
        return _lastSection;
    }

    //GetBlockState reads the state at the position, air outside the chunk height
    public BlockState GetBlockState(BlockPos pos)
        => GetSection(pos)?.GetBlockState(pos.X & 15, pos.Y & 15, pos.Z & 15) ?? default;

    public void Dispose()
    {
        _lastSection = null;
        _lastSectionY = int.MinValue;
    }
}
