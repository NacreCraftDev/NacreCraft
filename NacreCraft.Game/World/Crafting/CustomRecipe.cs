namespace NacreCraft.Game.World.Crafting;

//CustomRecipe special crafting recipe base class, maps to vanilla net.minecraft.world.item.crafting.CustomRecipe
//Custom recipes cannot be placed from the recipe book and never show a toast
public abstract class CustomRecipe : CraftingRecipe
{
    protected CustomRecipe()
        : base("", CraftingBookCategory.Misc.SerializedName, false)
    {
    }

    public bool IsSpecial => true;

    //Placement custom recipes cannot be placed, maps to vanilla placementInfo
    public PlacementInfo Placement => PlacementInfo.NotPlaceable;
}
