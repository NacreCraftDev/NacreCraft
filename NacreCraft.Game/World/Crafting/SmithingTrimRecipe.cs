using NacreCraft.Codec;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Crafting;

//SmithingTrimRecipe smithing trim recipe, maps to vanilla net.minecraft.world.item.crafting.SmithingTrimRecipe
//Applies an armor trim pattern to the base item; the trim component is not ported, so no trimmed item is produced yet
public sealed class SmithingTrimRecipe : SimpleSmithingRecipe
{
    public const string SerializerId = "smithing_trim";

    public SmithingTrimRecipe(bool showNotification, Ingredient template, Ingredient baseIngredient,
        Ingredient addition, Holder<TrimPattern> pattern)
        : base(showNotification)
    {
        Template = template;
        Base = baseIngredient;
        Addition = addition;
        Pattern = pattern;
    }

    public override string Type => SerializerId;

    public Ingredient Template { get; }
    public Ingredient Base { get; }
    public Ingredient Addition { get; }
    public Holder<TrimPattern> Pattern { get; }

    public override Optional<Ingredient> TemplateIngredient => Optional<Ingredient>.Of(Template);

    public override Ingredient BaseIngredient => Base;

    public override Optional<Ingredient> AdditionIngredient => Optional<Ingredient>.Of(Addition);

    public override ItemStack Assemble(SmithingRecipeInput input) => ItemStack.Empty;

    protected override PlacementInfo CreatePlacementInfo()
        => PlacementInfo.Create(new[] { Template, Base, Addition });
}
