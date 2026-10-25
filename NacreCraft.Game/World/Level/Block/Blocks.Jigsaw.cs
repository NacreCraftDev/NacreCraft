using NacreCraft.Game.Server;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using GameDirection = NacreCraft.Primitives.Direction;

namespace NacreCraft.Game.World.Level.Block;

//Blocks jigsaw block part; the block only carries the orientation state and creates its block entity
public static partial class Blocks
{
    //Jigsaw jigsaw block, maps to vanilla net.minecraft.world.level.block.JigsawBlock
    public sealed class Jigsaw : BlockBehaviour
    {
        //OrientationPropertyName orientation property name, maps to vanilla JigsawBlock.ORIENTATION
        public const string OrientationPropertyName = "orientation";

        public override Identifier Id => Identifier.WithDefaultNamespace("jigsaw");

        public override BlockEntity? CreateBlockEntity(BlockPos pos, BlockState state)
            => new JigsawBlockEntity(pos);

        //GetStateForPlacement faces the clicked side; a horizontal click keeps the top up, maps to vanilla getStateForPlacement
        public override BlockState? GetStateForPlacement(ServerLevel level, BlockPos pos, GameDirection face,
            GameDirection horizontalFacing)
        {
            var top = face.GetAxis() == GameDirection.Axis.Y ? horizontalFacing.Opposite : GameDirection.Up;
            var orientation = NacreCraft.Game.World.Level.LevelGen.Structure.JigsawBlock.FromFrontAndTop(face, top);
            foreach (var value in DefaultBlockState.GetValues())
            {
                if (value.Property.Name != OrientationPropertyName) continue;
                return DefaultBlockState.SetValue(value.Property, orientation);
            }
            return DefaultBlockState;
        }

        //UseOn lets the client open its jigsaw screen, maps to vanilla useWithoutItem
        public override bool UseOn(ServerLevel level, ServerPlayer player, BlockPos pos, BlockState state,
            NacreCraft.Primitives.Direction face)
            => player.HasPermissions((int)PermissionLevel.Gamemasters)
                && level.GetBlockEntity<JigsawBlockEntity>(pos) is not null;
    }

    public static readonly Jigsaw JIGSAW = new();

    //RegisterJigsaw puts the jigsaw block into the real block table
    private static void RegisterJigsaw(Dictionary<string, BlockBehaviour> real)
        => real[JIGSAW.Id.Path] = JIGSAW;
}
