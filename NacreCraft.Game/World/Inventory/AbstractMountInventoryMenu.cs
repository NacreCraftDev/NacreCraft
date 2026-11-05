using NacreCraft.Game.Server;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//AbstractMountInventoryMenu inventory menu of a rideable entity, maps to vanilla net.minecraft.world.inventory.AbstractMountInventoryMenu
//Slots 0/1 are saddle and body armor, the mount inventory follows; the player inventory is appended by the subclass
//The mount entity hierarchy is not ported, so the mount is a LivingEntity and the inventory change check is a no-op
public abstract class AbstractMountInventoryMenu : AbstractContainerMenu
{
    public const int SlotSaddle = 0;
    public const int SlotBodyArmor = 1;
    public const int SlotInventoryStart = 2;

    protected AbstractMountInventoryMenu(int containerId, PlayerInventory playerInventory,
        Container mountInventory, LivingEntity mount)
        : base(null, containerId)
    {
        OwnerInventory = playerInventory;
        MountContainer = mountInventory;
        Mount = mount;
    }

    protected Container MountContainer { get; }

    protected LivingEntity Mount { get; }

    protected abstract bool HasInventoryChanged(Container container);

    public override bool StillValid(ServerPlayer player)
        => !HasInventoryChanged(MountContainer) && !Mount.IsDeadOrDying;

    //GetInventorySize mount inventory slot count for the given column count
    public static int GetInventorySize(int inventoryColumns) => inventoryColumns * 3;

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        var playerContainerStart = SlotInventoryStart + MountContainer.Size;
        if (slotIndex < playerContainerStart)
        {
            if (!MoveItemStackTo(stack, playerContainerStart, Slots.Count, true)) return ItemStack.Empty;
        }
        else if (GetSlot(SlotBodyArmor).MayPlace(stack) && !GetSlot(SlotBodyArmor).HasItem())
        {
            if (!MoveItemStackTo(stack, SlotBodyArmor, SlotBodyArmor + 1, false)) return ItemStack.Empty;
        }
        else if (GetSlot(SlotSaddle).MayPlace(stack) && !GetSlot(SlotSaddle).HasItem())
        {
            if (!MoveItemStackTo(stack, SlotSaddle, SlotSaddle + 1, false)) return ItemStack.Empty;
        }
        else if (MountContainer.Size == 0
                 || !MoveItemStackTo(stack, SlotInventoryStart, playerContainerStart, false))
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }
}
