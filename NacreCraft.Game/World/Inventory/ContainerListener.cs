using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//ContainerListener menu change listener, maps to vanilla net.minecraft.world.inventory.ContainerListener
//A listener is notified on every synced slot change and data slot change; the client screen and the remote slot table both consume it
public interface ContainerListener
{
    void SlotChanged(AbstractContainerMenu menu, int slotIndex, ItemStack stack);

    void DataChanged(AbstractContainerMenu menu, int id, int value);
}
