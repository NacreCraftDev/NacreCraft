using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block.Dispenser;

//ShearsDispenseItemBehavior shears the entity in front, maps to vanilla net.minecraft.core.dispenser.ShearsDispenseItemBehavior
//The beehive branch is not ported: BeehiveBlock here only carries its registry type and has no honey level or bee release logic
public class ShearsDispenseItemBehavior : OptionalDispenseItemBehavior
{
    protected override ItemStack Execute(BlockSource source, ItemStack dispensed)
    {
        var level = source.Level;
        var pos = source.Pos.Offset(FacingOf(source.State).ToPrimitive());
        SetSuccess(TryShearEntity(level, pos, dispensed));
        if (IsSuccess()) ConsumeOne(dispensed);
        return dispensed;
    }

    //TryShearEntity shears the first shearable entity inside the cell, maps to vanilla tryShearEntity
    //Leash cutting is not ported because the leash system is not wired up
    private static bool TryShearEntity(ServerLevel level, BlockPos pos, ItemStack tool)
    {
        var area = new AABB(pos.X, pos.Y, pos.Z, pos.X + 1.0, pos.Y + 1.0, pos.Z + 1.0);
        var entities = level.GetEntitiesOfClass<NacreCraft.Registry.Entity>(area, entity => !entity.IsSpectator());
        foreach (var entity in entities)
        {
            if (entity.IsRemoved || entity is not Shearable shearable || !shearable.ReadyForShearing()) continue;
            shearable.Shear(level, SoundSource.Blocks, tool);
            level.GameEvent(null, GameEvent.Shear, pos);
            return true;
        }
        return false;
    }
}
