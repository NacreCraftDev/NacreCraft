using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Inventory;

//ArmorSlot equipment slot bound to a living entity, maps to vanilla net.minecraft.world.inventory.ArmorSlot
//Holds one item and reports the empty-slot sprite to the client
//Vanilla asks the entity whether the item fits the slot; the equippable component is not ported, so any item is accepted
public class ArmorSlot : Slot
{
    public ArmorSlot(Container container, LivingEntity owner, EquipmentSlot slot, int slotIndex, int x, int y,
        Identifier? emptyIcon)
        : base(container, slotIndex, x, y)
    {
        Owner = owner;
        Slot = slot;
        EmptyIcon = emptyIcon;
    }

    public LivingEntity Owner { get; }

    public EquipmentSlot Slot { get; }

    //EmptyIcon sprite shown while the slot is empty
    public Identifier? EmptyIcon { get; }

    public override int GetMaxStackSize() => 1;

    //IsActive whether the entity can currently use this slot
    public virtual bool IsActive() => true;
}
