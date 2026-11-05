using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//RepairItemRecipe repair two damaged items of the same type, maps to vanilla net.minecraft.world.item.crafting.RepairItemRecipe
//Combines both durabilities plus a 5% bonus; enchantment transfer is skipped because the enchantment component is not ported
public sealed class RepairItemRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_repairitem";

    public static readonly RepairItemRecipe Instance = new();

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input) => GetItemsToCombine(input) is not null;

    public override ItemStack Assemble(CraftingInput input)
    {
        var pair = GetItemsToCombine(input);
        if (pair is null) return ItemStack.Empty;
        var (first, second) = pair.Value;
        var firstMax = MaxDamageOf(first);
        var secondMax = MaxDamageOf(second);
        var durability = Math.Max(firstMax, secondMax);
        var remaining = firstMax - DamageOf(first) + secondMax - DamageOf(second) + durability * 5 / 100;
        var result = first.CopyWithCount(1);
        result.Set(DataComponents.MAX_DAMAGE, durability);
        result.Set(DataComponents.DAMAGE, Math.Max(durability - remaining, 0));
        return result;
    }

    private static (ItemStack First, ItemStack Second)? GetItemsToCombine(CraftingInput input)
    {
        if (input.IngredientCount != 2) return null;
        ItemStack? first = null;
        for (var i = 0; i < input.Size; i++)
        {
            var stack = input.GetItem(i);
            if (stack.IsEmpty()) continue;
            if (first is null)
            {
                first = stack;
                continue;
            }
            return CanCombine(first, stack) ? (first, stack) : null;
        }
        return null;
    }

    private static bool CanCombine(ItemStack first, ItemStack second)
        => ReferenceEquals(first.GetItem(), second.GetItem())
           && first.GetCount() == 1 && second.GetCount() == 1
           && first.Get(DataComponents.MAX_DAMAGE) is int
           && second.Get(DataComponents.MAX_DAMAGE) is int
           && first.Get(DataComponents.DAMAGE) is int
           && second.Get(DataComponents.DAMAGE) is int;

    private static int MaxDamageOf(ItemStack stack) => stack.Get(DataComponents.MAX_DAMAGE) as int? ?? 0;

    private static int DamageOf(ItemStack stack) => stack.Get(DataComponents.DAMAGE) as int? ?? 0;
}
