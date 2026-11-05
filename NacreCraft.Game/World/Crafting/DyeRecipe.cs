using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//DyeRecipe dye recipe, maps to vanilla net.minecraft.world.item.crafting.DyeRecipe
//One target item plus one or more dyes; vanilla reads the DYE component of each dye to compute the color
//The dye color component is not ported, so the result template is produced without recoloring
public sealed class DyeRecipe : NormalCraftingRecipe
{
    public const string SerializerId = "crafting_dye";

    public DyeRecipe(string group, string category, bool showNotification, Ingredient target,
        Ingredient dye, ItemStack result)
        : base(group, category, showNotification)
    {
        Target = target;
        Dye = dye;
        Result = result;
    }

    public Ingredient Target { get; }
    public Ingredient Dye { get; }
    public ItemStack Result { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        if (input.IngredientCount < 2) return false;
        var hasTarget = false;
        var hasDye = false;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (Target.Matches(stack))
            {
                if (hasTarget) return false;
                hasTarget = true;
                continue;
            }
            if (Dye.Matches(stack))
            {
                hasDye = true;
                continue;
            }
            return false;
        }
        return hasTarget && hasDye;
    }

    public override ItemStack Assemble(CraftingInput input) => Result.Copy();

    protected override PlacementInfo CreatePlacementInfo()
        => PlacementInfo.Create(new[] { Target, Dye });
}
