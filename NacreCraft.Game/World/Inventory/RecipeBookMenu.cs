using NacreCraft.Game.World.Crafting;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Inventory;

//RecipeBookMenu menu that supports placing a recipe from the recipe book, maps to vanilla net.minecraft.world.inventory.RecipeBookMenu
//The recipe book itself is not ported yet, implementations can return Nothing from HandlePlacement
public abstract class RecipeBookMenu : AbstractContainerMenu
{
    protected RecipeBookMenu(MenuType? menuType, int containerId)
        : base(menuType, containerId)
    {
    }

    public abstract PostPlaceAction HandlePlacement(bool useMaxItems, bool allowDroppingItemsToClear,
        RecipeHolder recipe, ServerLevel level, PlayerInventory inventory);

    public abstract void FillCraftSlotsStackedContents(StackedItemContents contents);

    public abstract RecipeBookType RecipeBookType { get; }

    //PostPlaceAction what the client should do after the recipe was placed, maps to vanilla PostPlaceAction
    public enum PostPlaceAction
    {
        Nothing,
        PlaceGhostRecipe,
    }
}
