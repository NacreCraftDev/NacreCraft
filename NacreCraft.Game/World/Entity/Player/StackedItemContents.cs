using NacreCraft.Game.World.Crafting;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//StackedItemContents the player-side ingredient multiset used by the recipe book, maps to vanilla net.minecraft.world.entity.player.StackedItemContents
//NC's Recipe interface does not expose PlacementInfo yet, so callers pass the placement info directly instead of a recipe
public sealed class StackedItemContents
{
    private readonly StackedContents<Holder<Item>> _raw = new();

    //AccountSimpleStack accounts only clean stacks, maps to vanilla accountSimpleStack
    public void AccountSimpleStack(ItemStack stack)
    {
        if (Inventory.IsUsableForCrafting(stack)) AccountStack(stack);
    }

    //AccountStack accounts a stack capped at its own stack size, maps to vanilla accountStack
    public void AccountStack(ItemStack stack) => AccountStack(stack, stack.GetMaxStackSize());

    public void AccountStack(ItemStack stack, int maxCount)
    {
        if (!stack.IsEmpty()) _raw.Account(stack.TypeHolder, Math.Min(maxCount, stack.Count));
    }

    //CanCraft whether the placement can be crafted once, maps to vanilla canCraft
    public bool CanCraft(PlacementInfo placement, StackedContents<Holder<Item>>.Output? output)
        => CanCraft(placement, 1, output);

    public bool CanCraft(PlacementInfo placement, int amount, StackedContents<Holder<Item>>.Output? output)
        => !placement.IsImpossibleToPlace && _raw.TryPick(BuildIngredients(placement), amount, output);

    public bool CanCraft(IReadOnlyList<StackedContents<Holder<Item>>.IngredientInfo> contents,
        StackedContents<Holder<Item>>.Output? output)
        => CanCraft(contents, 1, output);

    public bool CanCraft(IReadOnlyList<StackedContents<Holder<Item>>.IngredientInfo> contents, int amount,
        StackedContents<Holder<Item>>.Output? output)
        => _raw.TryPick(contents, amount, output);

    //GetBiggestCraftableStack largest amount that can be crafted, maps to vanilla getBiggestCraftableStack
    public int GetBiggestCraftableStack(PlacementInfo placement, StackedContents<Holder<Item>>.Output? output)
        => GetBiggestCraftableStack(placement, int.MaxValue, output);

    public int GetBiggestCraftableStack(PlacementInfo placement, int maxSize,
        StackedContents<Holder<Item>>.Output? output)
        => _raw.TryPickAll(BuildIngredients(placement), maxSize, output);

    public void Clear() => _raw.Clear();

    private static List<StackedContents<Holder<Item>>.IngredientInfo> BuildIngredients(PlacementInfo placement)
    {
        var result = new List<StackedContents<Holder<Item>>.IngredientInfo>(placement.Ingredients.Count);
        foreach (var ingredient in placement.Ingredients) result.Add(new IngredientInfo(ingredient));
        return result;
    }

    //IngredientInfo adapts a crafting Ingredient to the generic picker, maps to vanilla ingredient info
    private sealed class IngredientInfo(Ingredient ingredient) : StackedContents<Holder<Item>>.IngredientInfo
    {
        public bool AcceptsItem(Holder<Item> item)
            => ingredient.Matches(new ItemStack(item, 1, DataComponentPatch.Empty));
    }
}
