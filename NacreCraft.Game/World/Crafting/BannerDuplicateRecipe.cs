using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//BannerDuplicateRecipe banner duplication, maps to vanilla net.minecraft.world.item.crafting.BannerDuplicateRecipe
//Two banners of the same color produce a copy, one carrying the patterns
//The banner pattern layers component is not ported, so the source/target split is skipped and both banners are only compared by item
public sealed class BannerDuplicateRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_bannerduplicate";

    public BannerDuplicateRecipe(Ingredient banner, ItemStack result)
    {
        Banner = banner;
        Result = result;
    }

    public Ingredient Banner { get; }

    public ItemStack Result { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        if (input.IngredientCount != 2) return false;
        ItemStack? first = null;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (!Banner.Matches(stack)) return false;
            if (first is null)
            {
                first = stack;
                continue;
            }
            if (!ReferenceEquals(first.GetItem(), stack.GetItem())) return false;
        }
        return first is not null;
    }

    public override ItemStack Assemble(CraftingInput input) => Result.Copy();
}
