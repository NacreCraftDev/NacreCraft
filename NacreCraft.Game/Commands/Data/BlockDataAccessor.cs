using System.Globalization;
using NacreCraft.Commands;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.Commands.Data;

//BlockDataAccessor block entity data access, maps to vanilla net.minecraft.server.commands.data.BlockDataAccessor
//Takes the block entity's own NBT; the command errors when the target position has no block entity
public sealed class BlockDataAccessor : IDataAccessor
{
    //ErrorNotBlockEntity no block entity at the target position, maps to vanilla ERROR_NOT_A_BLOCK_ENTITY
    public static readonly SimpleCommandExceptionType ErrorNotBlockEntity =
        new(new LiteralMessage("no block entity at the target position"));

    private readonly PersistentServerLevel _level;
    private readonly PlayerList _players;
    private readonly BlockEntity _entity;
    private readonly BlockPos _pos;

    public BlockDataAccessor(PersistentServerLevel level, PlayerList players, BlockEntity entity, BlockPos pos)
    {
        _level = level;
        _players = players;
        _entity = entity;
        _pos = pos;
    }

    //SetData reads the block entity back whole, maps to vanilla setData's loadWithComponents + sendBlockUpdated
    //Vanilla marks dirty and broadcasts update 3 after writing; this broadcasts the latest data and notifies neighbors
    //Output strength such as comparators lives in the block entity; without notifying neighbors it is not re-evaluated
    public void SetData(CompoundTag tag)
    {
        _entity.LoadCustomOnly(tag);
        _players.BroadcastAll(_entity.GetUpdatePacket());
        if (_level.GetBlockState(_pos) is { } state) _level.UpdateNeighborsAt(_pos, state.Owner);
    }

    public CompoundTag GetData() => _entity.SaveWithFullMetadata();

    public string ModifiedSuccess => $"Modified block data of {Format()}";

    //PrintSuccess matches the vanilla message commands.data.block.query: "<x>, <y>, <z> has the following block data: <snbt>"
    public string PrintSuccess(Tag data)
        => $"{Format()} has the following block data: {NbtUtils.ToSnbt(data)}";

    public string PrintSuccess(NbtPath path, double scale, int value)
        => $"{path} on block {Format()} after scale factor of "
            + $"{scale.ToString("0.00", CultureInfo.InvariantCulture)} is {value}";

    //Format the position as the vanilla message placeholder list does, comma separated
    private string Format() => $"{_pos.X}, {_pos.Y}, {_pos.Z}";
}
