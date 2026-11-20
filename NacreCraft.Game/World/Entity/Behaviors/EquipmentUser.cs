using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Game.World.Items.Equipment;
using NacreCraft.Game.World.Loot;

namespace NacreCraft.Game.World.Entity;

//EquipmentUser an entity that can wear equipment from a loot table, maps to vanilla net.minecraft.world.entity.EquipmentUser
//Equip is degraded: NC has no reloadable registry lookup for a loot table key, so the slot resolution is implemented and the
//loot-table roll is left to the caller. The interface still gives mobs the vanilla equipment hooks
public interface EquipmentUser
{
    void SetItemSlot(EquipmentSlot slot, ItemStack stack);

    ItemStack GetItemBySlot(EquipmentSlot slot);

    void SetDropChance(EquipmentSlot slot, float dropChance);

    //Equip applies a rolled equipment table, maps to vanilla equip(EquipmentTable, LootParams)
    //The loot roll needs a loot table registry entry; when NC gains one this method resolves it and calls ResolveSlot per item
    void Equip(EquipmentTable equipment, LootParams lootParams)
    {
        _ = equipment;
        _ = lootParams;
    }

    //ResolveSlot picks the slot an item should be equipped into, maps to vanilla resolveSlot
    //Items without the equippable component go to the main hand; a slot already filled by this roll is skipped
    EquipmentSlot? ResolveSlot(ItemStack toEquip, IReadOnlyList<EquipmentSlot> alreadyInsertedIntoSlots)
    {
        if (toEquip.IsEmpty()) return null;
        var equippable = toEquip.Get(DataComponents.EQUIPPABLE) as Equippable;
        if (equippable is null)
            return alreadyInsertedIntoSlots.Contains(EquipmentSlot.MAINHAND) ? null : EquipmentSlot.MAINHAND;
        return alreadyInsertedIntoSlots.Contains(equippable.Slot) ? null : equippable.Slot;
    }
}
