using NacreCraft.Codec;

namespace NacreCraft.Game.World.Crafting;

//SmithingRecipe smithing recipe contract, maps to vanilla net.minecraft.world.item.crafting.SmithingRecipe
//Matching is shared: the base must match and the optional template/addition must match when present
public interface SmithingRecipe : Recipe<SmithingRecipeInput>
{
    Optional<Ingredient> TemplateIngredient { get; }

    Ingredient BaseIngredient { get; }

    Optional<Ingredient> AdditionIngredient { get; }
}
