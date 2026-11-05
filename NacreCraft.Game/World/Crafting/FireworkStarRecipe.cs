using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;

namespace NacreCraft.Game.World.Crafting;

//FireworkStarRecipe firework star recipe, maps to vanilla net.minecraft.world.item.crafting.FireworkStarRecipe
//Gunpowder, a shape item, a dye and optionally trail and twinkle items form the star
//Vanilla reads the dye color from the DYE component; that component is not ported, so stars get the default white color
public sealed class FireworkStarRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_firework_star";

    //DefaultColor color used when no dye color can be read
    public const int DefaultColor = 0xFFFFFF;

    public FireworkStarRecipe(IReadOnlyDictionary<FireworkExplosion.ShapeKind, Ingredient> shapes,
        Ingredient trail, Ingredient twinkle, Ingredient fuel, Ingredient dye, ItemStack result)
    {
        Shapes = shapes;
        Trail = trail;
        Twinkle = twinkle;
        Fuel = fuel;
        Dye = dye;
        Result = result;
    }

    public IReadOnlyDictionary<FireworkExplosion.ShapeKind, Ingredient> Shapes { get; }
    public Ingredient Trail { get; }
    public Ingredient Twinkle { get; }
    public Ingredient Fuel { get; }
    public Ingredient Dye { get; }
    public ItemStack Result { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        if (input.IngredientCount < 2) return false;
        var hasFuel = false;
        var hasDye = false;
        var hasShape = false;
        var hasTrail = false;
        var hasTwinkle = false;
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (Twinkle.Matches(stack))
            {
                if (hasTwinkle) return false;
                hasTwinkle = true;
                continue;
            }
            if (Trail.Matches(stack))
            {
                if (hasTrail) return false;
                hasTrail = true;
                continue;
            }
            if (Fuel.Matches(stack))
            {
                if (hasFuel) return false;
                hasFuel = true;
                continue;
            }
            if (Dye.Matches(stack))
            {
                hasDye = true;
                continue;
            }
            if (FindShape(stack) is not null)
            {
                if (hasShape) return false;
                hasShape = true;
                continue;
            }
            return false;
        }
        return hasFuel && hasDye;
    }

    //Assemble builds the explosion component, maps to vanilla assemble
    public override ItemStack Assemble(CraftingInput input)
    {
        var shape = FireworkExplosion.ShapeKind.Small;
        var hasTwinkle = false;
        var hasTrail = false;
        var colors = new List<int>();
        for (var slot = 0; slot < input.Size; slot++)
        {
            var stack = input.GetItem(slot);
            if (stack.IsEmpty()) continue;
            if (FindShape(stack) is { } foundShape)
            {
                shape = foundShape;
                continue;
            }
            if (Twinkle.Matches(stack))
            {
                hasTwinkle = true;
                continue;
            }
            if (Trail.Matches(stack))
            {
                hasTrail = true;
                continue;
            }
            if (Dye.Matches(stack)) colors.Add(DefaultColor);
        }
        var star = Result.Copy();
        star.Set(DataComponents.FIREWORK_EXPLOSION,
            new FireworkExplosion(shape, colors, Array.Empty<int>(), hasTrail, hasTwinkle));
        return star;
    }

    private FireworkExplosion.ShapeKind? FindShape(ItemStack stack)
    {
        foreach (var entry in Shapes)
            if (entry.Value.Matches(stack)) return entry.Key;
        return null;
    }
}
