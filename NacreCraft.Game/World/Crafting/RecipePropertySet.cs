using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Crafting;

//RecipePropertySet the set of items a recipe slot accepts, maps to vanilla net.minecraft.world.item.crafting.RecipePropertySet
//The smithing and furnace menus read these sets instead of scanning the recipe table
public sealed class RecipePropertySet
{
    //TypeKey the registry key the sets are loaded under, maps to vanilla TYPE_KEY
    public static readonly Identifier TypeKey = Identifier.WithDefaultNamespace("recipe_property_set");

    public static readonly Identifier SmithingBase = Identifier.WithDefaultNamespace("smithing_base");
    public static readonly Identifier SmithingTemplate = Identifier.WithDefaultNamespace("smithing_template");
    public static readonly Identifier SmithingAddition = Identifier.WithDefaultNamespace("smithing_addition");
    public static readonly Identifier FurnaceInput = Identifier.WithDefaultNamespace("furnace_input");
    public static readonly Identifier BlastFurnaceInput = Identifier.WithDefaultNamespace("blast_furnace_input");
    public static readonly Identifier SmokerInput = Identifier.WithDefaultNamespace("smoker_input");
    public static readonly Identifier CampfireInput = Identifier.WithDefaultNamespace("campfire_input");

    //Empty set that accepts nothing
    public static readonly RecipePropertySet Empty = new(new HashSet<Holder<Item>>());

    private readonly HashSet<Holder<Item>> _items;

    private RecipePropertySet(HashSet<Holder<Item>> items) => _items = items;

    public IReadOnlyCollection<Holder<Item>> Items => _items;

    //Test whether the stack's item belongs to the set, maps to vanilla test
    public bool Test(ItemStack stack)
        => stack.GetTypeHolder() is { } holder && _items.Contains(holder);

    //Create collects the items of every ingredient, maps to vanilla create
    public static RecipePropertySet Create(IEnumerable<Ingredient> ingredients)
    {
        var items = new HashSet<Holder<Item>>();
        foreach (var ingredient in ingredients)
            foreach (var holder in ingredient.Values)
                items.Add(holder);
        return new RecipePropertySet(items);
    }
}
