using NacreCraft.Registry;

namespace NacreCraft.Game.World.Crafting;

//RecipeMap recipes indexed by type and id, maps to vanilla net.minecraft.world.item.crafting.RecipeMap
//NC recipes carry their type as a string, so the by-type index is keyed by that string
public sealed class RecipeMap
{
    public static readonly RecipeMap Empty = new(new Dictionary<string, List<RecipeHolder>>(),
        new Dictionary<Identifier, RecipeHolder>());

    private readonly Dictionary<string, List<RecipeHolder>> _byType;
    private readonly Dictionary<Identifier, RecipeHolder> _byKey;

    private RecipeMap(Dictionary<string, List<RecipeHolder>> byType, Dictionary<Identifier, RecipeHolder> byKey)
    {
        _byType = byType;
        _byKey = byKey;
    }

    public IReadOnlyCollection<RecipeHolder> Values => _byKey.Values;

    //Create builds the indexes from a recipe collection, maps to vanilla create
    public static RecipeMap Create(IEnumerable<RecipeHolder> recipes)
    {
        var byType = new Dictionary<string, List<RecipeHolder>>();
        var byKey = new Dictionary<Identifier, RecipeHolder>();
        foreach (var holder in recipes)
        {
            if (!byType.TryGetValue(holder.Value.Type, out var list))
                byType[holder.Value.Type] = list = new List<RecipeHolder>();
            list.Add(holder);
            byKey[holder.Id] = holder;
        }
        return new RecipeMap(byType, byKey);
    }

    public IReadOnlyList<RecipeHolder> ByType(string type)
        => _byType.TryGetValue(type, out var list) ? list : Array.Empty<RecipeHolder>();

    public RecipeHolder? ByKey(Identifier id) => _byKey.GetValueOrDefault(id);

    //GetRecipesFor returns the crafting recipes of the type matching the input, maps to vanilla getRecipesFor
    public IEnumerable<RecipeHolder> GetRecipesFor(string type, CraftingInput input)
    {
        if (input.IsEmpty) return Enumerable.Empty<RecipeHolder>();
        return ByType(type).Where(holder => holder.Value is CraftingRecipe recipe && recipe.Matches(input));
    }
}
