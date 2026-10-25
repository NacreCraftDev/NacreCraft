using NacreCraft.Game.Server;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//Blocks structure block part; the block only holds the mode state and forwards redstone triggers
public static partial class Blocks
{
    //StructureBlock structure block, maps to vanilla net.minecraft.world.level.block.StructureBlock
    public sealed class StructureBlock : BlockBehaviour
    {
        //ModePropertyName mode property name, maps to vanilla StructureBlock.MODE
        public const string ModePropertyName = "mode";

        public override Identifier Id => Identifier.WithDefaultNamespace("structure_block");

        public override BlockEntity? CreateBlockEntity(BlockPos pos, BlockState state)
            => new StructureBlockEntity(pos, state);

        //SetPlacedBy records who placed the block as the template author, maps to vanilla setPlacedBy
        public override void SetPlacedBy(ServerLevel level, BlockPos pos, BlockState state, ServerPlayer player)
            => level.GetBlockEntity<StructureBlockEntity>(pos)?.SetAuthor(player.Profile.Name);

        //UseOn lets the client open its structure block screen; the server only checks the permission, maps to vanilla usedBy
        public override bool UseOn(ServerLevel level, ServerPlayer player, BlockPos pos, BlockState state,
            NacreCraft.Primitives.Direction face)
            => player.HasPermissions((int)PermissionLevel.Gamemasters)
                && level.GetBlockEntity<StructureBlockEntity>(pos) is not null;

        //NeighborChanged turns the redstone rising edge into one save or load, maps to vanilla neighborChanged
        public override void NeighborChanged(ServerLevel level, BlockPos pos, BlockState state,
            Registry.Block changedBlock, bool movedByPiston)
        {
            if (level.GetBlockEntity<StructureBlockEntity>(pos) is not { } structureBlock) return;
            var shouldTrigger = level.HasNeighborSignal(pos);
            var powered = structureBlock.Powered;
            if (shouldTrigger && !powered)
            {
                structureBlock.SetPowered(true);
                Trigger(level, structureBlock);
            }
            else if (!shouldTrigger && powered)
            {
                structureBlock.SetPowered(false);
            }
        }

        //Trigger runs the mode-specific action of one redstone pulse, maps to vanilla trigger
        private static void Trigger(ServerLevel level, StructureBlockEntity structureBlock)
        {
            switch (structureBlock.Mode)
            {
                case StructureMode.save:
                    structureBlock.SaveStructure(false);
                    break;
                case StructureMode.load:
                    structureBlock.PlaceStructure(level);
                    break;
                case StructureMode.corner:
                    structureBlock.UnloadStructure();
                    break;
            }
        }
    }

    public static readonly StructureBlock STRUCTURE_BLOCK = new();

    //RegisterStructureBlock puts the structure block into the real block table
    private static void RegisterStructureBlock(Dictionary<string, BlockBehaviour> real)
        => real[STRUCTURE_BLOCK.Id.Path] = STRUCTURE_BLOCK;
}
