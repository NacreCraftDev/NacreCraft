using NacreCraft.Game.World.Level.Block;

namespace NacreCraft.Game.World.Inventory;

//SmokerMenu smoker menu, maps to vanilla net.minecraft.world.inventory.SmokerMenu
//Identical to the furnace layout, only the menu type and cooking recipe type differ
public sealed class SmokerMenu : AbstractFurnaceMenu
{
    public SmokerMenu(int containerId, PlayerInventory inventory, AbstractFurnaceBlockEntity furnace)
        : base(MenuTypes.SMOKER, containerId, inventory, furnace)
    {
    }
}
