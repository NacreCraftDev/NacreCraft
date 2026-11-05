namespace NacreCraft.Game.World.Inventory;

//PlayerEnderChestContainer the player's ender chest inventory, maps to vanilla net.minecraft.world.inventory.PlayerEnderChestContainer
//27 slots shared by every ender chest the player opens
//Vanilla tracks the active chest block entity to forward open/close; the block entity side is not wired here
public sealed class PlayerEnderChestContainer() : SimpleContainer(27)
{
}
