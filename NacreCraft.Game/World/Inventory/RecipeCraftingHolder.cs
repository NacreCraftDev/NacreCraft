using NacreCraft.Game.World.Crafting;

namespace NacreCraft.Game.World.Inventory;

//RecipeCraftingHolder container that remembers the recipe used for its result, maps to vanilla net.minecraft.world.inventory.RecipeCraftingHolder
//The recipe book and the crafted-stat trigger read it back after a result is taken
//Vanilla awards recipes to the player here; the recipe book is not ported yet, so only the slot state is kept
public interface RecipeCraftingHolder
{
    void SetRecipeUsed(RecipeHolder? recipe);

    RecipeHolder? GetRecipeUsed();
}
