namespace NacreCraft.Game.World.Inventory;

//StackedContentsCompatible container that can report its contents to the recipe book, maps to vanilla net.minecraft.world.inventory.StackedContentsCompatible
public interface StackedContentsCompatible
{
    void FillStackedContents(StackedItemContents contents);
}
