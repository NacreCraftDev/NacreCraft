using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//DecoratedPotRecipe decorated pot recipe, maps to vanilla net.minecraft.world.item.crafting.DecoratedPotRecipe
//Four sherds on the sides of a 3x3 grid form the pot
//The pot decorations component is not ported, so the result template carries no decoration data
public sealed class DecoratedPotRecipe : CustomRecipe
{
    public const string SerializerId = "crafting_special_decorated_pot";

    public DecoratedPotRecipe(Ingredient wallPattern, ItemStack result)
        : this(wallPattern, wallPattern, wallPattern, wallPattern, result)
    {
    }

    public DecoratedPotRecipe(Ingredient backPattern, Ingredient leftPattern, Ingredient rightPattern,
        Ingredient frontPattern, ItemStack result)
    {
        BackPattern = backPattern;
        LeftPattern = leftPattern;
        RightPattern = rightPattern;
        FrontPattern = frontPattern;
        Result = result;
    }

    public Ingredient BackPattern { get; }
    public Ingredient LeftPattern { get; }
    public Ingredient RightPattern { get; }
    public Ingredient FrontPattern { get; }
    public ItemStack Result { get; }

    public override string Type => SerializerId;

    public override bool Matches(CraftingInput input)
    {
        if (input.Width != 3 || input.Height != 3 || input.IngredientCount != 4) return false;
        return BackPattern.Matches(input.GetItem(1, 0))
            && LeftPattern.Matches(input.GetItem(0, 1))
            && RightPattern.Matches(input.GetItem(2, 1))
            && FrontPattern.Matches(input.GetItem(1, 2));
    }

    public override ItemStack Assemble(CraftingInput input) => Result.Copy();
}
