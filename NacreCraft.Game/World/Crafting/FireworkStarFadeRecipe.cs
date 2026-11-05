using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;

namespace NacreCraft.Game.World.Crafting;

//FireworkStarFadeRecipe firework star fade recipe, maps to vanilla net.minecraft.world.item.crafting.FireworkStarFadeRecipe
//Adds fade colors to an existing star; the dye color is not readable here, so the default white fade color is appended
public sealed class FireworkStarFadeRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_firework_star_fade";

    public FireworkStarFadeRecipe(Ingredient target, Ingredient dye, ItemStack result)
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
            if (Dye.Matches(stack))
            {
                hasDye = true;
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
        return hasTarget && hasDye;
    }

    //Assemble appends the fade color to the star's explosion, maps to vanilla assemble
    public override ItemStack Assemble(CraftingInput input)
    {
        ItemStack? target = null;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (!stack.IsEmpty() && Target.Matches(stack)) target = stack;
        }
        if (target is null) return ItemStack.Empty;
        var explosion = target.Get(DataComponents.FIREWORK_EXPLOSION) as FireworkExplosion
            ?? new FireworkExplosion(FireworkExplosion.ShapeKind.Small, Array.Empty<int>(),
                Array.Empty<int>(), false, false);
        var fadeColors = new List<int>(explosion.FadeColors) { FireworkStarRecipe.DefaultColor };
        var result = target.CopyWithCount(1);
        result.Set(DataComponents.FIREWORK_EXPLOSION,
            new FireworkExplosion(explosion.Shape, explosion.Colors, fadeColors, explosion.Trail, explosion.Twinkle));
        return result;
    }
}
