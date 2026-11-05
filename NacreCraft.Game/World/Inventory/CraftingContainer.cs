using NacreCraft.Game.World.Crafting;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//CraftingContainer crafting grid container, maps to vanilla net.minecraft.world.inventory.CraftingContainer
//Adds the grid dimensions and item list on top of Container so a recipe input can be built from it
public interface CraftingContainer : Container, StackedContentsCompatible
{
    int Width { get; }

    int Height { get; }

    IReadOnlyList<ItemStack> Items { get; }

    //AsCraftInput builds the recipe input, maps to vanilla asCraftInput
    CraftingInput AsCraftInput() => CraftingInput.Of(Width, Height, Items);
}
