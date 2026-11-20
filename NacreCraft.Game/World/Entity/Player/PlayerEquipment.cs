using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Entity;

//PlayerEquipment the equipment view of a player, maps to vanilla net.minecraft.world.entity.player.PlayerEquipment
//The main-hand slot is not a separate slot: reads and writes go to the selected hotbar slot, so holding a sword and selecting
//another hotbar slot change the same equipment entry
public sealed class PlayerEquipment(Player player) : EntityEquipment
{
    public Player Player => player;

    public override ItemStack Get(EquipmentSlot slot)
        => slot == EquipmentSlot.MAINHAND ? player.Inventory.SelectedItem : base.Get(slot);

    public override void Set(EquipmentSlot slot, ItemStack stack)
    {
        if (slot == EquipmentSlot.MAINHAND) player.Inventory.SelectedItem = stack;
        else base.Set(slot, stack);
    }

    public override bool IsEmpty() => player.Inventory.SelectedItem.IsEmpty() && base.IsEmpty();
}
