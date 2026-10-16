using NacreCraft.Registry.State;
using NacreCraft.Storage.Chunk;

namespace NacreCraft.Storage;

//BlockGetter, block read interface, maps to vanilla net.minecraft.world.level.BlockGetter
//Vanilla extends LevelHeightAccessor; the light engine also gets the world height range through it
public interface BlockGetter : LevelHeightAccessor
{
    //getBlockState returns the block state at world coords; out of range returns air
    BlockState GetBlockState(int x, int y, int z);
}
