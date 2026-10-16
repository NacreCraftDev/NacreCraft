using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//ISculkBlockAccess the block read and write surface the sculk spreader needs, maps to the overlapping part of
//vanilla LevelAccessor (a live level) and WorldGenLevel (world generation)
//The spreader is one algorithm driven from two places, so both sides adapt to this rather than keeping a copy each
public interface ISculkBlockAccess
{
    //GetBlockState the state at a position, null when the chunk is not loaded
    BlockState? GetBlockState(BlockPos pos);

    //SetBlock write a state and report whether it landed, maps to vanilla LevelWriter.setBlock
    bool SetBlock(BlockPos pos, BlockState state);

    //LevelEvent broadcast a level event; world generation has no client to tell, so the default does nothing
    void LevelEvent(int type, BlockPos pos, int data)
    {
    }
}

//ServerLevelSculkAccess adapts a live level to the block access, maps to vanilla LevelAccessor
public sealed class ServerLevelSculkAccess(ServerLevel level) : ISculkBlockAccess
{
    public BlockState? GetBlockState(BlockPos pos) => level.GetBlockState(pos);

    public bool SetBlock(BlockPos pos, BlockState state) => level.SetBlock(pos, state);

    public void LevelEvent(int type, BlockPos pos, int data) => level.LevelEvent(type, pos, data);
}

//WorldGenRegionSculkAccess adapts the world generation region to the block access, maps to vanilla WorldGenLevel
public sealed class WorldGenRegionSculkAccess(WorldGenRegion level) : ISculkBlockAccess
{
    public BlockState? GetBlockState(BlockPos pos) => level.GetBlockState(pos.X, pos.Y, pos.Z);

    //The region write hands back the state it displaced, not a success flag, so the write always reports as landed
    //maps to vanilla WorldGenLevel.setBlock, which always returns true
    public bool SetBlock(BlockPos pos, BlockState state)
    {
        level.SetBlockState(pos.X, pos.Y, pos.Z, state);
        return true;
    }
}
