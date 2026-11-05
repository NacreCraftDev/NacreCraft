using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level;

//PathNavigationRegion chunk snapshot used by pathfinding, maps to vanilla net.minecraft.world.level.PathNavigationRegion
//The vanilla Level type does not exist here, so the region keeps a ServerLevel; unloaded chunks read as air
//The empty Y-space probe and the empty-chunk placeholder are not ported, a null chunk answers air instead
public sealed class PathNavigationRegion : CollisionGetter
{
    protected readonly int CenterX;
    protected readonly int CenterZ;
    protected readonly ChunkAccess?[][] Chunks;
    protected readonly ServerLevel Level;
    protected bool AllEmpty;

    public PathNavigationRegion(ServerLevel level, BlockPos start, BlockPos end)
    {
        Level = level;
        CenterX = SectionPos.BlockToSectionCoord(start.X);
        CenterZ = SectionPos.BlockToSectionCoord(start.Z);
        var maxX = SectionPos.BlockToSectionCoord(end.X);
        var maxZ = SectionPos.BlockToSectionCoord(end.Z);
        Chunks = new ChunkAccess?[maxX - CenterX + 1][];
        for (var x = 0; x < Chunks.Length; x++)
            Chunks[x] = new ChunkAccess?[maxZ - CenterZ + 1];
        for (var chunkX = CenterX; chunkX <= maxX; chunkX++)
        for (var chunkZ = CenterZ; chunkZ <= maxZ; chunkZ++)
            Chunks[chunkX - CenterX][chunkZ - CenterZ] = level.GetChunk(new ChunkPos(chunkX, chunkZ));

        AllEmpty = true;
        foreach (var row in Chunks)
        foreach (var chunk in row)
        {
            if (chunk is null) continue;
            AllEmpty = false;
            return;
        }
    }

    public WorldBorder GetWorldBorder() => Level.WorldBorder;

    //GetChunkForCollisions answers this region because ChunkAccess has no BlockGetter view in this project
    //Maps to vanilla getChunkForCollisions, degraded until chunks expose block reads through BlockGetter
    public BlockGetter GetChunkForCollisions(int chunkX, int chunkZ) => this;

    public BlockState GetBlockState(BlockPos pos)
    {
        if (pos.Y < Level.MinBuildHeight || pos.Y >= Level.MaxBuildHeight) return default;
        return GetChunk(pos)?.GetBlockState(pos.X, pos.Y, pos.Z) ?? default;
    }

    public BlockState GetBlockState(int x, int y, int z) => GetBlockState(new BlockPos(x, y, z));

    //GetFluidState derives the fluid from the block state because ChunkAccess exposes no fluid read
    //Maps to vanilla getFluidState, degraded until chunks carry fluid states
    public FluidState GetFluidState(BlockPos pos) => GetBlockState(pos).FluidState;

    public int MinSectionY => Level.MinBuildHeight / 16;

    public int SectionsCount => (Level.MaxBuildHeight - Level.MinBuildHeight) / 16;

    public int MaxSectionY => MinSectionY + SectionsCount - 1;

    private ChunkAccess? GetChunk(BlockPos pos)
        => GetChunk(SectionPos.BlockToSectionCoord(pos.X), SectionPos.BlockToSectionCoord(pos.Z));

    private ChunkAccess? GetChunk(int chunkX, int chunkZ)
    {
        var x = chunkX - CenterX;
        var z = chunkZ - CenterZ;
        if (x < 0 || x >= Chunks.Length || z < 0 || z >= Chunks[x].Length) return null;
        return Chunks[x][z];
    }
}
