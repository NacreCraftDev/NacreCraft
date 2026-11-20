using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Entity;

//IEquipmentHolder entity holding equipment slots, maps to the capability of vanilla LivingEntity.getItemBySlot
//Implemented by LivingEntity; Mob and Player inherit it
public interface IEquipmentHolder
{
    //GetItemBySlot returns the item in the given slot, empty slots give an empty stack
    ItemStack GetItemBySlot(EquipmentSlot slot);
}

//EntityEquipment equipment slot container, the eight slots are indexed by EquipmentSlot id
//PlayerEquipment overrides the main-hand routing so the hotbar slot and the equipment view stay in sync
public class EntityEquipment
{
    private readonly ItemStack[] _slots = new ItemStack[8];

    public EntityEquipment()
    {
        for (var i = 0; i < _slots.Length; i++) _slots[i] = ItemStack.Empty;
    }

    //Get returns the slot item, empty slots give an empty stack
    public virtual ItemStack Get(EquipmentSlot slot) => _slots[(int)slot];

    //Set writes the slot
    public virtual void Set(EquipmentSlot slot, ItemStack stack) => _slots[(int)slot] = stack;

    //IsEmpty whether every slot is empty, maps to vanilla isEmpty
    public virtual bool IsEmpty()
    {
        foreach (var stack in _slots)
        {
            if (!stack.IsEmpty()) return false;
        }
        return true;
    }

    //Clear empties every slot, maps to vanilla clear
    public virtual void Clear()
    {
        for (var i = 0; i < _slots.Length; i++) _slots[i] = ItemStack.Empty;
    }
}
