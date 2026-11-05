using NacreCraft.Registry;

namespace NacreCraft.Game.World.Crafting;

//RecipeBookCategories built-in recipe book category instances, maps to vanilla net.minecraft.world.item.crafting.RecipeBookCategories
//The registry has no recipes yet, so the categories are registered lazily on first access in vanilla declaration order
public static class RecipeBookCategories
{
    public static readonly RecipeBookCategory CraftingBuildingBlocks = Register("crafting_building_blocks");
    public static readonly RecipeBookCategory CraftingRedstone = Register("crafting_redstone");
    public static readonly RecipeBookCategory CraftingEquipment = Register("crafting_equipment");
    public static readonly RecipeBookCategory CraftingMisc = Register("crafting_misc");
    public static readonly RecipeBookCategory FurnaceFood = Register("furnace_food");
    public static readonly RecipeBookCategory FurnaceBlocks = Register("furnace_blocks");
    public static readonly RecipeBookCategory FurnaceMisc = Register("furnace_misc");
    public static readonly RecipeBookCategory BlastFurnaceBlocks = Register("blast_furnace_blocks");
    public static readonly RecipeBookCategory BlastFurnaceMisc = Register("blast_furnace_misc");
    public static readonly RecipeBookCategory SmokerFood = Register("smoker_food");
    public static readonly RecipeBookCategory Stonecutter = Register("stonecutter");
    public static readonly RecipeBookCategory Smithing = Register("smithing");
    public static readonly RecipeBookCategory Campfire = Register("campfire");

    private static RecipeBookCategory Register(string id)
    {
        var category = new Category();
        Registry<RecipeBookCategory>.Register(BuiltInRegistries.RECIPE_BOOK_CATEGORY, id, category);
        return category;
    }

    //Bootstrap forces the static initialization, maps to vanilla bootstrap
    public static RecipeBookCategory Bootstrap() => Campfire;

    //Category plain category instance, maps to vanilla new RecipeBookCategory()
    private sealed class Category : RecipeBookCategory
    {
    }
}
