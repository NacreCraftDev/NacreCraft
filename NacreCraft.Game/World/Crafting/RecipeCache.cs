using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Crafting;

//RecipeCache last-hit cache for crafting lookups, maps to vanilla net.minecraft.world.item.crafting.RecipeCache
//Entries are compared by grid dimensions and one-of-each item, a hit moves to the front so the next lookup is O(1)
public sealed class RecipeCache
{
    private sealed record Entry(ItemStack[] Key, int Width, int Height, RecipeHolder? Value);

    private readonly Entry?[] _entries;
    private RecipeManager? _cachedManager;

    public RecipeCache(int capacity) => _entries = new Entry?[capacity];

    //Get looks the recipe up in the cache, computing it on a miss, maps to vanilla get
    public RecipeHolder? Get(CraftingInput input)
    {
        if (input.IsEmpty) return null;
        ValidateRecipeManager();
        for (var i = 0; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            if (entry is null || !Matches(entry, input)) continue;
            MoveEntryToFront(i);
            return entry.Value;
        }
        return Compute(input);
    }

    private void ValidateRecipeManager()
    {
        var manager = RecipeManager.Active;
        if (ReferenceEquals(manager, _cachedManager)) return;
        _cachedManager = manager;
        Array.Clear(_entries);
    }

    private RecipeHolder? Compute(CraftingInput input)
    {
        var recipe = RecipeManager.Active?.FindCraftingRecipe(input);
        Insert(input, recipe);
        return recipe;
    }

    private void MoveEntryToFront(int index)
    {
        if (index <= 0) return;
        var entry = _entries[index];
        Array.Copy(_entries, 0, _entries, 1, index);
        _entries[0] = entry;
    }

    private void Insert(CraftingInput input, RecipeHolder? recipe)
    {
        var key = new ItemStack[input.Size];
        for (var i = 0; i < key.Length; i++) key[i] = input.GetItem(i).CopyWithCount(1);
        Array.Copy(_entries, 0, _entries, 1, _entries.Length - 1);
        _entries[0] = new Entry(key, input.Width, input.Height, recipe);
    }

    private static bool Matches(Entry entry, CraftingInput input)
    {
        if (entry.Width != input.Width || entry.Height != input.Height) return false;
        for (var i = 0; i < entry.Key.Length; i++)
            if (!SameStack(entry.Key[i], input.GetItem(i))) return false;
        return true;
    }

    private static bool SameStack(ItemStack first, ItemStack second)
    {
        if (first.IsEmpty() || second.IsEmpty()) return first.IsEmpty() && second.IsEmpty();
        return first.IsSameItemAndComponentsAs(second);
    }
}
