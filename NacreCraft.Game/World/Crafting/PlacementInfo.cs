using NacreCraft.Codec;

namespace NacreCraft.Game.World.Crafting;

//PlacementInfo how a recipe is placed into the crafting grid, maps to vanilla net.minecraft.world.item.crafting.PlacementInfo
//SlotsToIngredientIndex marks each grid slot with an ingredient index, -1 means the slot stays empty
public sealed class PlacementInfo
{
    public const int EmptySlot = -1;

    //NotPlaceable a recipe that cannot be placed from the recipe book
    public static readonly PlacementInfo NotPlaceable = new(Array.Empty<Ingredient>(), Array.Empty<int>());

    private PlacementInfo(IReadOnlyList<Ingredient> ingredients, IReadOnlyList<int> slotsToIngredientIndex)
    {
        Ingredients = ingredients;
        SlotsToIngredientIndex = slotsToIngredientIndex;
    }

    public IReadOnlyList<Ingredient> Ingredients { get; }

    public IReadOnlyList<int> SlotsToIngredientIndex { get; }

    public bool IsImpossibleToPlace => SlotsToIngredientIndex.Count == 0;

    public static PlacementInfo Create(Ingredient ingredient)
        => ingredient.IsEmpty ? NotPlaceable : new PlacementInfo(new[] { ingredient }, new[] { 0 });

    public static PlacementInfo Create(IReadOnlyList<Ingredient> ingredients)
    {
        var indices = new int[ingredients.Count];
        for (var i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i].IsEmpty) return NotPlaceable;
            indices[i] = i;
        }
        return new PlacementInfo(ingredients, indices);
    }

    public static PlacementInfo CreateFromOptionals(IReadOnlyList<Optional<Ingredient>> ingredients)
    {
        var present = new List<Ingredient>(ingredients.Count);
        var indices = new int[ingredients.Count];
        var placementIndex = 0;
        for (var i = 0; i < ingredients.Count; i++)
        {
            if (!ingredients[i].IsPresent)
            {
                indices[i] = EmptySlot;
                continue;
            }
            var ingredient = ingredients[i].Get();
            if (ingredient.IsEmpty) return NotPlaceable;
            present.Add(ingredient);
            indices[i] = placementIndex++;
        }
        return new PlacementInfo(present, indices);
    }
}
