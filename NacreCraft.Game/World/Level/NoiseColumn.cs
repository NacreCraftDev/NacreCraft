using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level;

//NoiseColumn a fixed vertical block run used during noise generation, maps to vanilla net.minecraft.world.level.NoiseColumn
//Out-of-range reads answer the default air state; writes outside the column throw like vanilla
public sealed class NoiseColumn : BlockColumn
{
    private readonly int _minY;
    private readonly BlockState[] _column;

    public NoiseColumn(int minY, BlockState[] column)
    {
        _minY = minY;
        _column = column;
    }

    public BlockState GetBlock(int blockY)
    {
        var index = blockY - _minY;
        return index < 0 || index >= _column.Length ? default : _column[index];
    }

    public void SetBlock(int blockY, BlockState state)
    {
        var index = blockY - _minY;
        if (index < 0 || index >= _column.Length)
            throw new ArgumentOutOfRangeException(nameof(blockY), $"Outside of column height: {blockY}");
        _column[index] = state;
    }
}
