using NacreCraft.Registry;

namespace NacreCraft.Game.World.Crafting;

//RecipeAccess what a menu may read from the recipe table, maps to vanilla net.minecraft.world.item.crafting.RecipeAccess
//Property sets drive slot placement rules, the stonecutter set drives its selection list
public interface RecipeAccess
{
    RecipePropertySet PropertySet(Identifier key);

    SelectableRecipe.SingleInputSet StonecutterRecipes();
}
