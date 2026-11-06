using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block.Dispenser;

//EquipmentDispenseItemBehavior equips an item onto a living entity in front, maps to vanilla net.minecraft.core.dispenser.EquipmentDispenseItemBehavior
//An entity that cannot take the item makes the dispenser throw it instead
public class EquipmentDispenseItemBehavior : DefaultDispenseItemBehavior
{
    public static readonly EquipmentDispenseItemBehavior Instance = new();

    protected override ItemStack Execute(BlockSource source, ItemStack dispensed)
        => DispenseEquipment(source, dispensed) ? dispensed : base.Execute(source, dispensed);

    //DispenseEquipment equips one item onto the first entity in front that accepts it, maps to vanilla dispenseEquipment
    public static bool DispenseEquipment(BlockSource source, ItemStack dispensed)
    {
        var pos = source.Pos.Offset(FacingOf(source.State).ToPrimitive());
        var area = new AABB(pos.X, pos.Y, pos.Z, pos.X + 1.0, pos.Y + 1.0, pos.Z + 1.0);
        var entities = source.Level.GetEntitiesOfClass<LivingEntity>(area, entity => entity.CanEquipWithDispenser(dispensed));
        if (entities.Count == 0) return false;
        var target = entities[0];
        var slot = target.GetEquipmentSlotForItem(dispensed);
        target.SetItemSlot(slot, SplitOne(dispensed));
        if (target is Mob mob)
        {
            mob.SetGuaranteedDrop(slot);
            mob.SetPersistenceRequired();
        }
        return true;
    }
}
