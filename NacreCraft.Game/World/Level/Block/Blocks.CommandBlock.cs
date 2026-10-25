using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//Blocks command block part; the three command block variants share one behavior class and differ by mode
public static partial class Blocks
{
    //CommandBlock command block, maps to vanilla CommandBlock / ChainCommandBlock / RepeatingCommandBlock
    //The block only creates the block entity and forwards redstone changes; execution lives in CommandBlockEntity
    public sealed class CommandBlock : BlockBehaviour
    {
        private readonly string _path;
        private readonly CommandBlockEntity.Mode _mode;

        public CommandBlock(string path, CommandBlockEntity.Mode mode)
        {
            _path = path;
            _mode = mode;
        }

        public override Identifier Id => Identifier.WithDefaultNamespace(_path);

        //CreateBlockEntity command blocks always carry their command state in a block entity, maps to vanilla newBlockEntity
        public override BlockEntity? CreateBlockEntity(BlockPos pos, BlockState state)
            => new CommandBlockEntity(pos, _mode);

        //NeighborChanged forwards the redstone state; the entity turns a rising edge into one execution
        public override void NeighborChanged(ServerLevel level, BlockPos pos, BlockState state,
            NacreCraft.Registry.Block changedBlock, bool movedByPiston)
            => level.GetBlockEntity<CommandBlockEntity>(pos)?.SetPowered(level.HasNeighborSignal(pos));
    }

    public static readonly CommandBlock COMMAND_BLOCK =
        new("command_block", CommandBlockEntity.Mode.Impulse);

    public static readonly CommandBlock CHAIN_COMMAND_BLOCK =
        new("chain_command_block", CommandBlockEntity.Mode.Chain);

    public static readonly CommandBlock REPEATING_COMMAND_BLOCK =
        new("repeating_command_block", CommandBlockEntity.Mode.Repeating);

    //RegisterCommandBlocks puts the three command blocks into the real block table
    private static void RegisterCommandBlocks(Dictionary<string, BlockBehaviour> real)
    {
        real[COMMAND_BLOCK.Id.Path] = COMMAND_BLOCK;
        real[CHAIN_COMMAND_BLOCK.Id.Path] = CHAIN_COMMAND_BLOCK;
        real[REPEATING_COMMAND_BLOCK.Id.Path] = REPEATING_COMMAND_BLOCK;
    }
}
