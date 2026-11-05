using NacreCraft.Codec;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//SmithingTransformRecipe smithing transform recipe, maps to vanilla net.minecraft.world.item.crafting.SmithingTransformRecipe
//Template, base and addition produce the result; vanilla keeps the base's components, which is not expressible for a different item here
public sealed class SmithingTransformRecipe : SimpleSmithingRecipe
{
    public const string SerializerId = "smithing_transform";

    public SmithingTransformRecipe(bool showNotification, Optional<Ingredient> template, Ingredient baseIngredient,
        Optional<Ingredient> addition, ItemStack result)
        : base(showNotification)
    {
        Template = template;
        Base = baseIngredient;
        Addition = addition;
        Result = result;
    }

    public override string Type => SerializerId;

    public Optional<Ingredient> Template { get; }
    public Ingredient Base { get; }
    public Optional<Ingredient> Addition { get; }
    public ItemStack Result { get; }

    public override Optional<Ingredient> TemplateIngredient => Template;

    public override Ingredient BaseIngredient => Base;

    public override Optional<Ingredient> AdditionIngredient => Addition;

    public override ItemStack Assemble(SmithingRecipeInput input) => Result.Copy();

    protected override PlacementInfo CreatePlacementInfo()
        => PlacementInfo.CreateFromOptionals(new[] { Template, Optional<Ingredient>.Of(Base), Addition });
}
