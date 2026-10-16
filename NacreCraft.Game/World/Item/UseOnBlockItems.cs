using NacreCraft.Game.Server;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Storage.Updates;

namespace NacreCraft.Game.World.Items;

//IUseOnBlockItem an item usable on a block, maps to vanilla Item.useOn
//Only reached when the block's own behavior did not handle it; flint and steel lighting fires goes here and future hoe tilling will use the same path
public interface IUseOnBlockItem
{
    //UseOn returns whether it was handled; when handled the block item placement path is skipped
    bool UseOn(PersistentServerLevel level, ServerPlayer player, ItemStack stack, BlockPos pos, Direction face);
}

//FlintAndSteelItem flint and steel lighting a fire on a block face, maps to vanilla net.minecraft.world.item.FlintAndSteelItem
//Vanilla has a separate branch lighting campfires and candles that depends on the LIT property and block behavior; nc has none of that, so only fire placement is done here
public sealed class FlintAndSteelItem(string name) : Item, IUseOnBlockItem
{
    public override Identifier Id => Identifier.WithDefaultNamespace(name);

    public bool UseOn(PersistentServerLevel level, ServerPlayer player, ItemStack stack, BlockPos pos, Direction face)
    {
        //Flint and steel on TNT sets the charge off rather than lighting a fire beside it
        //maps to the TNT branch of vanilla FlintAndSteelItem.useOn, which shares the ignition path with the block
        //The igniter is not credited: the server player object and the registry entity are separate hierarchies here
        if (level.GetBlockState(pos) is { } target && target.Owner is Blocks.TntBlock
            && Blocks.TntBlock.Prime(level, pos, null))
        {
            level.SetBlock(pos, Blocks.AIR.DefaultBlockState, BlockUpdateFlags.All);
            if (player.GameType != GameType.Creative)
            {
                stack.SetCount(stack.GetCount() - 1);
                player.ContainerMenu?.SendAllDataToRemote();
            }
            return true;
        }
        //The fire lands one block outside the clicked face, maps to vanilla relative(clickedFace)
        var fireTarget = pos.Offset(face);
        if (!Blocks.BaseFireBlock.CanBePlacedAt(level, fireTarget)) return false;
        level.SetBlock(fireTarget, Blocks.BaseFireBlock.GetState(level, fireTarget), BlockUpdateFlags.All);
        //Vanilla calls hurtAndBreak to damage the item; nc has no durability system, so a non-creative use consumes one
        if (player.GameType != GameType.Creative)
        {
            stack.SetCount(stack.GetCount() - 1);
            player.ContainerMenu?.SendAllDataToRemote();
        }
        return true;
    }
}
