using NacreCraft.Codec;

namespace NacreCraft.Game.World.Crafting;

//CraftingBookCategory crafting recipe book tab, maps to vanilla net.minecraft.world.item.crafting.CraftingBookCategory
//Vanilla is an enum; C# enums cannot carry the codec, so this is a value class with fixed instances
public sealed class CraftingBookCategory
{
    public static readonly CraftingBookCategory Building = new("building", 0);
    public static readonly CraftingBookCategory Redstone = new("redstone", 1);
    public static readonly CraftingBookCategory Equipment = new("equipment", 2);
    public static readonly CraftingBookCategory Misc = new("misc", 3);

    private static readonly CraftingBookCategory[] Values = { Building, Redstone, Equipment, Misc };

    //Codec persistence codec, serialized by name, maps to vanilla CODEC
    public static readonly Codec<CraftingBookCategory> Codec = Codecs.String.ComapFlatMap(
        name => Parse(name) is { } category
            ? DataResult<CraftingBookCategory>.Success(category)
            : DataResult<CraftingBookCategory>.Error(() => $"Unknown crafting book category: {name}"),
        category => category.SerializedName);

    private CraftingBookCategory(string serializedName, int id)
    {
        SerializedName = serializedName;
        Id = id;
    }

    public string SerializedName { get; }

    public int Id { get; }

    //ById looks up by the numeric id, out of range falls back to misc, maps to vanilla BY_ID
    public static CraftingBookCategory ById(int id) => id is >= 0 and < 4 ? Values[id] : Misc;

    private static CraftingBookCategory? Parse(string name)
    {
        foreach (var value in Values)
            if (value.SerializedName == name) return value;
        return null;
    }

    public override string ToString() => SerializedName;
}
