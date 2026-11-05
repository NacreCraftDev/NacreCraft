using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//TransmuteRecipe transmute recipe, maps to vanilla net.minecraft.world.item.crafting.TransmuteRecipe
//One input item plus 1..8 materials produces the result
//Vanilla keeps the input's components on the result; applying a component patch to another item is not expressible here, so the result template is used as-is
public sealed class TransmuteRecipe : NormalCraftingRecipe
{
    public const string SerializerId = "crafting_transmute";

    public static readonly MinMaxBounds.Ints DefaultMaterialCount = new(1, 1);
    public static readonly MinMaxBounds.Ints FullRangeMaterialCount = new(1, 8);

    public TransmuteRecipe(string group, string category, bool showNotification, Ingredient input,
        Ingredient material, MinMaxBounds.Ints materialCount, ItemStack result, bool addMaterialCountToResult)
        : base(group, category, showNotification)
    {
        Input = input;
        Material = material;
        MaterialCount = materialCount;
        Result = result;
        AddMaterialCountToResult = addMaterialCountToResult;
    }

    public Ingredient Input { get; }
    public Ingredient Material { get; }
    public MinMaxBounds.Ints MaterialCount { get; }
    public ItemStack Result { get; }
    public bool AddMaterialCountToResult { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        var minMaterials = MaterialCount.Min ?? 1;
        var maxMaterials = MaterialCount.Max ?? 8;
        if (input.IngredientCount < minMaterials + 1 || input.IngredientCount > maxMaterials + 1) return false;
        var foundInput = false;
        var materialCount = 0;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (Input.Matches(stack))
            {
                if (foundInput) return false;
                foundInput = true;
                continue;
            }
            if (Material.Matches(stack))
            {
                if (++materialCount > maxMaterials) return false;
                continue;
            }
            return false;
        }
        return foundInput && MaterialCount.Matches(materialCount);
    }

    //Assemble produces the result template, adding the material count when configured, maps to vanilla assemble
    public override ItemStack Assemble(CraftingInput input)
    {
        var resultCount = AddMaterialCountToResult ? Result.GetCount() + CountMaterials(input) : Result.GetCount();
        return Result.CopyWithCount(resultCount);
    }

    private int CountMaterials(CraftingInput input)
    {
        var count = 0;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (!stack.IsEmpty() && Material.Matches(stack)) count++;
        }
        return count;
    }

    protected override PlacementInfo CreatePlacementInfo()
    {
        var maxMaterials = MaterialCount.Max ?? 8;
        var ingredients = new List<Ingredient>(1 + maxMaterials) { Input };
        for (var i = 0; i < maxMaterials; i++) ingredients.Add(Material);
        return PlacementInfo.Create(ingredients);
    }
}
