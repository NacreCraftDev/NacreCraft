using NacreCraft.Game.World.Crafting;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//ResultContainer single result slot container, maps to vanilla net.minecraft.world.inventory.ResultContainer
//Unlike a plain SimpleContainer it also remembers the recipe that produced the result
public sealed class ResultContainer : Container, RecipeCraftingHolder
{
    private ItemStack _item = ItemStack.Empty;
    private RecipeHolder? _recipeUsed;

    public int Size => 1;

    public bool IsEmpty() => _item.IsEmpty();

    public ItemStack GetItem(int slot) => _item;

    //RemoveItem takes the whole slot regardless of the requested count, maps to vanilla ContainerHelper.takeItem
    public ItemStack RemoveItem(int slot, int count)
    {
        var taken = _item;
        _item = ItemStack.Empty;
        return taken;
    }

    public ItemStack RemoveItemNoUpdate(int slot) => RemoveItem(slot, 1);

    public void SetItem(int slot, ItemStack stack) => _item = stack ?? ItemStack.Empty;

    public void SetChanged() { }

    public void ClearContent() => _item = ItemStack.Empty;

    public bool CanPlaceItem(int slot, ItemStack stack) => true;

    public void SetRecipeUsed(RecipeHolder? recipe) => _recipeUsed = recipe;

    public RecipeHolder? GetRecipeUsed() => _recipeUsed;
}
