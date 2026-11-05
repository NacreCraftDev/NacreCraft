using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//ImbueRecipe imbue recipe, maps to vanilla net.minecraft.world.item.crafting.ImbueRecipe
//A 3x3 grid: the center source item keeps its potion contents and the eight surrounding materials form the result
public sealed class ImbueRecipe : NormalCraftingRecipe
{
    public const string SerializerId = "crafting_imbue";

    public ImbueRecipe(string group, string category, bool showNotification, Ingredient source,
        Ingredient material, ItemStack result)
        : base(group, category, showNotification)
    {
        Source = source;
        Material = material;
        Result = result;
    }

    public Ingredient Source { get; }

    public Ingredient Material { get; }

    public ItemStack Result { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        if (input.Width != 3 || input.Height != 3 || input.IngredientCount != 9) return false;
        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
            {
                var stack = input.GetItem(x, y);
                if (stack.IsEmpty()) return false;
                var ingredient = x == 1 && y == 1 ? Source : Material;
                if (!ingredient.Matches(stack)) return false;
            }
        return true;
    }

    //Assemble copies the result and carries the source potion contents over, maps to vanilla assemble
    public override ItemStack Assemble(CraftingInput input)
    {
        var result = Result.Copy();
        var contents = input.GetItem(1, 1).Get(DataComponents.POTION_CONTENTS);
        if (contents is not null) result.Set(DataComponents.POTION_CONTENTS, contents);
        return result;
    }

    protected override PlacementInfo CreatePlacementInfo()
        => PlacementInfo.Create(new[]
        {
            Material, Material, Material,
            Material, Source, Material,
            Material, Material, Material,
        });
}
