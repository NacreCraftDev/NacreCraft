using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Updates;

namespace NacreCraft.Game.World.Level.Block.Dispenser;

//FlintAndSteelDispenseItemBehavior lights the block in front, maps to vanilla net.minecraft.core.dispenser.FlintAndSteelDispenseItemBehavior
//Covers fire, campfires and TNT; candles and the sulfur cube branch wait for those blocks and mobs to be ported
public class FlintAndSteelDispenseItemBehavior : OptionalDispenseItemBehavior
{
    protected override ItemStack Execute(BlockSource source, ItemStack dispensed)
    {
        var level = source.Level;
        SetSuccess(true);
        var facing = FacingOf(source.State).ToPrimitive();
        var targetPos = source.Pos.Offset(facing);
        var target = level.GetBlockState(targetPos);
        if (Blocks.BaseFireBlock.CanBePlacedAt(level, targetPos))
        {
            level.SetBlock(targetPos, Blocks.BaseFireBlock.GetState(level, targetPos), BlockUpdateFlags.All);
            level.GameEvent(null, NacreCraft.Registry.GameEvent.BlockPlace, targetPos);
        }
        else if (target is { } campfire && Blocks.CampfireBlock.CanLight(campfire))
        {
            level.SetBlock(targetPos, campfire.SetValue(BlockStateProperties.Lit, true), BlockUpdateFlags.All);
            level.GameEvent(null, NacreCraft.Registry.GameEvent.BlockChange, targetPos);
        }
        else if (target?.Owner is Blocks.TntBlock)
        {
            if (Blocks.TntBlock.Prime(level, targetPos, null))
                level.SetBlock(targetPos, Blocks.AIR.DefaultBlockState, BlockUpdateFlags.All);
            else
                SetSuccess(false);
        }
        if (IsSuccess()) ConsumeOne(dispensed);
        return dispensed;
    }
}
