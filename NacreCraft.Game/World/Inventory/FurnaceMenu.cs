using NacreCraft.Game.World.Level.Block;

namespace NacreCraft.Game.World.Inventory;

//FurnaceMenu furnace menu, maps to vanilla net.minecraft.world.inventory.FurnaceMenu
//Shares the layout and quick move with the blast furnace and smoker through AbstractFurnaceMenu
public sealed class FurnaceMenu : AbstractFurnaceMenu
{
    private FurnaceMenu(MenuType kind, int containerId, PlayerInventory inventory,
        AbstractFurnaceBlockEntity furnace)
        : base(kind, containerId, inventory, furnace)
    {
    }

    public static FurnaceMenu ForFurnace(int containerId, PlayerInventory inventory,
        AbstractFurnaceBlockEntity furnace)
        => new(MenuTypes.FURNACE, containerId, inventory, furnace);

    public static FurnaceMenu ForBlastFurnace(int containerId, PlayerInventory inventory,
        AbstractFurnaceBlockEntity furnace)
        => new(MenuTypes.BLAST_FURNACE, containerId, inventory, furnace);

    public static FurnaceMenu ForSmoker(int containerId, PlayerInventory inventory,
        AbstractFurnaceBlockEntity furnace)
        => new(MenuTypes.SMOKER, containerId, inventory, furnace);
}
