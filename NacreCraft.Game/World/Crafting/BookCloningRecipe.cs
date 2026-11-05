using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//BookCloningRecipe written book cloning, maps to vanilla net.minecraft.world.item.crafting.BookCloningRecipe
//One written book plus materials produces copies; the generation limit is read from the written book content
//Content generation is not exposed by the ported component, so only the presence of content is checked
public sealed class BookCloningRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_bookcloning";

    public static readonly MinMaxBounds.Ints AllowedBookGenerationRanges = new(0, 2);
    public static readonly MinMaxBounds.Ints DefaultBookGenerationRanges = new(0, 1);

    public BookCloningRecipe(Ingredient source, Ingredient material, MinMaxBounds.Ints allowedGenerations,
        ItemStack result)
    {
        Source = source;
        Material = material;
        AllowedGenerations = allowedGenerations;
        Result = result;
    }

    public Ingredient Source { get; }
    public Ingredient Material { get; }
    public MinMaxBounds.Ints AllowedGenerations { get; }
    public ItemStack Result { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        if (input.IngredientCount < 2) return false;
        var hasSource = false;
        var hasMaterial = false;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (Source.Matches(stack))
            {
                if (hasSource || stack.Get(DataComponents.WRITTEN_BOOK_CONTENT) is null) return false;
                hasSource = true;
                continue;
            }
            if (Material.Matches(stack))
            {
                hasMaterial = true;
                continue;
            }
            return false;
        }
        return hasSource && hasMaterial;
    }

    //Assemble copies the source content onto the result template, maps to vanilla assemble
    public override ItemStack Assemble(CraftingInput input)
    {
        ItemStack? source = null;
        var materialCount = 0;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (Source.Matches(stack) && stack.Get(DataComponents.WRITTEN_BOOK_CONTENT) is { } content)
            {
                if (source is not null) return ItemStack.Empty;
                source = stack;
                continue;
            }
            if (Material.Matches(stack))
            {
                materialCount++;
                continue;
            }
            return ItemStack.Empty;
        }
        if (source is null) return ItemStack.Empty;
        var result = Result.CopyWithCount(Math.Max(1, materialCount));
        var sourceContent = source.Get(DataComponents.WRITTEN_BOOK_CONTENT);
        if (sourceContent is not null) result.Set(DataComponents.WRITTEN_BOOK_CONTENT, sourceContent);
        return result;
    }
}
