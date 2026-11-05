using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;

namespace NacreCraft.Game.World.Crafting;

//FireworkRocketRecipe firework rocket recipe, maps to vanilla net.minecraft.world.item.crafting.FireworkRocketRecipe
//One shell, one to three gunpowder and any number of firework stars form the rocket
public sealed class FireworkRocketRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_firework_rocket";

    public FireworkRocketRecipe(Ingredient shell, Ingredient fuel, Ingredient star, ItemStack result)
    {
        Shell = shell;
        Fuel = fuel;
        Star = star;
        Result = result;
    }

    public Ingredient Shell { get; }
    public Ingredient Fuel { get; }
    public Ingredient Star { get; }
    public ItemStack Result { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        if (input.IngredientCount < 2) return false;
        var hasShell = false;
        var fuelCount = 0;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (Shell.Matches(stack))
            {
                if (hasShell) return false;
                hasShell = true;
                continue;
            }
            if (Fuel.Matches(stack))
            {
                if (++fuelCount > 3) return false;
                continue;
            }
            if (!Star.Matches(stack)) return false;
        }
        return hasShell && fuelCount >= 1;
    }

    //Assemble collects the star explosions into the rockets component, maps to vanilla assemble
    public override ItemStack Assemble(CraftingInput input)
    {
        var explosions = new List<FireworkExplosion>();
        var fuelCount = 0;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (Fuel.Matches(stack))
            {
                fuelCount++;
                continue;
            }
            if (Star.Matches(stack) && stack.Get(DataComponents.FIREWORK_EXPLOSION) is FireworkExplosion explosion)
                explosions.Add(explosion);
        }
        var result = Result.Copy();
        result.Set(DataComponents.FIREWORKS, new Fireworks(fuelCount, explosions));
        return result;
    }
}
