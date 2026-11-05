using NacreCraft.Game.Server;

namespace NacreCraft.Game.World.Inventory;

//MenuConstructor builds a menu for a player, maps to vanilla net.minecraft.world.inventory.MenuConstructor
//MenuProvider uses it to defer menu creation until the open packet is sent
public delegate AbstractContainerMenu? MenuConstructor(int containerId, PlayerInventory inventory, ServerPlayer player);
