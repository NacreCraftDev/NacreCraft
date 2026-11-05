using NacreCraft.Game.World.Level.Block;

namespace NacreCraft.Game.World.Inventory;

//BlastFurnaceMenu blast furnace menu, maps to vanilla net.minecraft.world.inventory.BlastFurnaceMenu
//Identical to the furnace layout, only the menu type and cooking recipe type differ
public sealed class BlastFurnaceMenu : AbstractFurnaceMenu
{
    public BlastFurnaceMenu(int containerId, PlayerInventory inventory, AbstractFurnaceBlockEntity furnace)
        : base(MenuTypes.BLAST_FURNACE, containerId, inventory, furnace)
    {
    }
}
