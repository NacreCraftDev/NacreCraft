using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//MapExtendingRecipe map extending recipe, maps to vanilla net.minecraft.world.item.crafting.MapExtendingRecipe
//A filled map surrounded by eight materials produces a map of the next scale
//Map data is not ported, so the filled-map and scale checks are skipped and the pattern alone decides
public sealed class MapExtendingRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_mapextending";

    public MapExtendingRecipe(Ingredient map, Ingredient material, ItemStack result)
    {
        Map = map;
        Material = material;
        Result = result;
        var ingredients = new Ingredient?[9];
        for (var i = 0; i < ingredients.Length; i++) ingredients[i] = material;
        ingredients[4] = map;
        Pattern = new ShapedRecipePattern(3, 3, ingredients);
    }

    public Ingredient Map { get; }
    public Ingredient Material { get; }
    public ItemStack Result { get; }
    public ShapedRecipePattern Pattern { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input) => Pattern.Matches(input);

    public override ItemStack Assemble(CraftingInput input) => Result.Copy();
}
