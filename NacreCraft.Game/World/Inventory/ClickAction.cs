namespace NacreCraft.Game.World.Inventory;

//ClickAction which mouse button a click used, maps to vanilla net.minecraft.world.inventory.ClickAction
//The client serializes it into the click packet and the server restores the carried item from it
public enum ClickAction
{
    Primary,
    Secondary,
}
