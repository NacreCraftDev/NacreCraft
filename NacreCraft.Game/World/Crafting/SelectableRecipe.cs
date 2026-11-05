using NacreCraft.Codec;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//SelectableRecipe a recipe entry the client may choose from, maps to vanilla net.minecraft.world.item.crafting.SelectableRecipe
//OptionDisplay is the icon shown in the list; the display model is not ported, so it is kept as an opaque object
public sealed record SelectableRecipe(object? OptionDisplay, Optional<RecipeHolder> Recipe)
{
    //NoRecipe a display-only entry
    public static SelectableRecipe NoRecipe(object? optionDisplay)
        => new(optionDisplay, Optional<RecipeHolder>.Empty());

    //SingleInputSet recipes selectable from one input item, maps to vanilla SingleInputSet
    public sealed record SingleInputSet(IReadOnlyList<SingleInputEntry> Entries)
    {
        public static readonly SingleInputSet Empty = new(Array.Empty<SingleInputEntry>());

        public bool AcceptsInput(ItemStack input)
        {
            foreach (var entry in Entries)
                if (entry.Input.Matches(input)) return true;
            return false;
        }

        //SelectByInput keeps the entries whose ingredient matches, maps to vanilla selectByInput
        public SingleInputSet SelectByInput(ItemStack input)
        {
            var matches = new List<SingleInputEntry>();
            foreach (var entry in Entries)
                if (entry.Input.Matches(input)) matches.Add(entry);
            return new SingleInputSet(matches);
        }

        public bool IsEmpty => Entries.Count == 0;

        public int Size => Entries.Count;
    }

    //SingleInputEntry one selectable entry, maps to vanilla SingleInputEntry
    public sealed record SingleInputEntry(Ingredient Input, SelectableRecipe Recipe);
}
