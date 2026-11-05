using NacreCraft.Codec;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//SimpleSmithingRecipe smithing recipe base class, maps to vanilla net.minecraft.world.item.crafting.SimpleSmithingRecipe
//Caches the placement info; every simple smithing recipe has an empty group
public abstract class SimpleSmithingRecipe : SmithingRecipe
{
    private PlacementInfo? _placementInfo;

    protected SimpleSmithingRecipe(bool showNotification) => ShowNotification = showNotification;

    public bool ShowNotification { get; }

    public string Group => "";

    public abstract string Type { get; }

    //Matches shared matching: the base must match and the optional template/addition must match when present
    public bool Matches(SmithingRecipeInput input)
        => (!TemplateIngredient.IsPresent || TemplateIngredient.Get().Matches(input.Template))
           && BaseIngredient.Matches(input.Base)
           && (!AdditionIngredient.IsPresent || AdditionIngredient.Get().Matches(input.Addition));

    public abstract Optional<Ingredient> TemplateIngredient { get; }

    public abstract Ingredient BaseIngredient { get; }

    public abstract Optional<Ingredient> AdditionIngredient { get; }

    public abstract ItemStack Assemble(SmithingRecipeInput input);

    public PlacementInfo Placement => _placementInfo ??= CreatePlacementInfo();

    protected abstract PlacementInfo CreatePlacementInfo();
}
