using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Storage;
using NacreCraft.Storage.Updates;
//Both Primitives and Registry.Enums define Direction; this behavior only needs the primitive one
using Direction = NacreCraft.Primitives.Direction;

namespace NacreCraft.Game.World.Level.Block.Dispenser;

//ShulkerBoxDispenseBehavior places a block item in front, maps to vanilla net.minecraft.core.dispenser.ShulkerBoxDispenseBehavior
//Reduced BlockItem.place: the target must be replaceable, the placement state is computed from the dispenser direction and a failed placement leaves the item alone
//The entity obstruction check is skipped because a dispenser has no placer entity
public class ShulkerBoxDispenseBehavior : OptionalDispenseItemBehavior
{
    protected override ItemStack Execute(BlockSource source, ItemStack dispensed)
    {
        SetSuccess(false);
        if (dispensed.GetItem() is not BlockItem blockItem) return dispensed;
        var level = source.Level;
        var facing = FacingOf(source.State).ToPrimitive();
        var relativePos = source.Pos.Offset(facing);
        var below = level.GetBlockState(relativePos.Offset(0, -1, 0));
        var clickedFace = below is null || below.Value.Owner.IsAir ? facing : Direction.Up;
        var target = level.GetBlockState(relativePos);
        if (target is { } targetState && targetState.Owner is BlockBehaviour targetBehaviour && !targetBehaviour.CanBeReplaced)
            return dispensed;
        var placedBlock = blockItem.PlacedBlock;
        var placedState = placedBlock is BlockBehaviour behaviour
            ? behaviour.GetStateForPlacement(level, relativePos, clickedFace, facing)
            : placedBlock.DefaultBlockState;
        if (placedState is not { } state) return dispensed;
        if (state.Owner is BlockBehaviour placementBehaviour && !placementBehaviour.CanSurvive(level, relativePos, state))
            return dispensed;
        if (!level.SetBlock(relativePos, state, BlockUpdateFlags.All)) return dispensed;
        dispensed.Shrink(1);
        SetSuccess(true);
        return dispensed;
    }
}
