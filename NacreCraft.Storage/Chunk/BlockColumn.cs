using NacreCraft.Registry.State;

namespace NacreCraft.Storage;

//BlockColumn a vertical run of block states, maps to vanilla net.minecraft.world.level.chunk.BlockColumn
//Readers that walk a column, such as heightmap priming, only need these two members
public interface BlockColumn
{
    BlockState GetBlock(int y);

    void SetBlock(int y, BlockState state);
}
