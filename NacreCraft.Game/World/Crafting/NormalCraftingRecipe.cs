namespace NacreCraft.Game.World.Crafting;

//NormalCraftingRecipe recipe that can be placed from the recipe book, maps to vanilla net.minecraft.world.item.crafting.NormalCraftingRecipe
//Caches the placement info built from the ingredients
public abstract class NormalCraftingRecipe : CraftingRecipe
{
    private PlacementInfo? _placementInfo;

    protected NormalCraftingRecipe(string group, string category, bool showNotification)
        : base(group, category, showNotification)
    {
    }

    //Placement lazy placement info, maps to vanilla placementInfo
    public PlacementInfo Placement => _placementInfo ??= CreatePlacementInfo();

    protected abstract PlacementInfo CreatePlacementInfo();
}
