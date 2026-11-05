using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Inventory;

//NautilusInventoryMenu nautilus inventory menu, maps to vanilla net.minecraft.world.inventory.NautilusInventoryMenu
//Saddle and nautilus armor slots are backed by their own one-slot containers; the nautilus entity is not ported
public sealed class NautilusInventoryMenu : AbstractMountInventoryMenu
{
    public NautilusInventoryMenu(int containerId, PlayerInventory playerInventory, Container nautilusInventory,
        LivingEntity nautilus, int inventoryColumns)
        : base(containerId, playerInventory, nautilusInventory, nautilus)
    {
        var saddleSprite = Identifier.WithDefaultNamespace("container/slot/saddle");
        var armorSprite = Identifier.WithDefaultNamespace("container/slot/nautilus_armor_inventory");
        AddSlot(new ArmorSlot(new SimpleContainer(1), nautilus, EquipmentSlot.SADDLE, 0, 8, 18, saddleSprite));
        AddSlot(new ArmorSlot(new SimpleContainer(1), nautilus, EquipmentSlot.BODY, 0, 8, 36, armorSprite));
        for (var y = 0; y < 3; y++)
            for (var x = 0; x < inventoryColumns; x++)
                AddSlot(new Slot(nautilusInventory, x + y * inventoryColumns, 80 + x * 18, 18 + y * 18));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(playerInventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(playerInventory, i, 8 + i * 18, 142));
    }

    //HasInventoryChanged the nautilus inventory change check needs AbstractNautilus, which is not ported
    protected override bool HasInventoryChanged(Container container) => false;
}
