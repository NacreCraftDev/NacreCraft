using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Crafting;

//SmithingRecipeInput smithing table input, maps to vanilla net.minecraft.world.item.crafting.SmithingRecipeInput
//Three slots: template, base and addition
public sealed record SmithingRecipeInput(ItemStack Template, ItemStack Base, ItemStack Addition) : RecipeInput
{
    public int Size => 3;

    public bool IsEmpty => Template.IsEmpty() && Base.IsEmpty() && Addition.IsEmpty();

    public ItemStack GetItem(int index) => index switch
    {
        0 => Template,
        1 => Base,
        2 => Addition,
        _ => throw new ArgumentOutOfRangeException(nameof(index), $"Recipe does not contain slot {index}"),
    };
}
