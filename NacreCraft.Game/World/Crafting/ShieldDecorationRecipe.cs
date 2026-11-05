using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//ShieldDecorationRecipe shield decoration, maps to vanilla net.minecraft.world.item.crafting.ShieldDecorationRecipe
//A patterned banner decorates a plain shield; the banner pattern layers component is not ported, so only the item types are checked
public sealed class ShieldDecorationRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_shielddecoration";

    public ShieldDecorationRecipe(Ingredient banner, Ingredient target, ItemStack result)
    {
        Banner = banner;
        Target = target;
        Result = result;
    }

    public Ingredient Banner { get; }
    public Ingredient Target { get; }
    public ItemStack Result { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        if (input.IngredientCount != 2) return false;
        var hasBanner = false;
        var hasTarget = false;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (Banner.Matches(stack))
            {
                if (hasBanner) return false;
                hasBanner = true;
                continue;
            }
            if (Target.Matches(stack))
            {
                if (hasTarget) return false;
                hasTarget = true;
                continue;
            }
            return false;
        }
        return hasBanner && hasTarget;
    }

    public override ItemStack Assemble(CraftingInput input) => Result.Copy();
}
