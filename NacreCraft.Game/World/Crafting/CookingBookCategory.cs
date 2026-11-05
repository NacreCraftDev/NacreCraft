using NacreCraft.Codec;

namespace NacreCraft.Game.World.Crafting;

//CookingBookCategory cooking recipe book tab, maps to vanilla net.minecraft.world.item.crafting.CookingBookCategory
//Vanilla is an enum; C# enums cannot carry the codec, so this is a value class with fixed instances
public sealed class CookingBookCategory
{
    public static readonly CookingBookCategory Food = new("food", 0);
    public static readonly CookingBookCategory Blocks = new("blocks", 1);
    public static readonly CookingBookCategory Misc = new("misc", 2);

    private static readonly CookingBookCategory[] Values = { Food, Blocks, Misc };

    //Codec persistence codec, serialized by name, maps to vanilla CODEC
    public static readonly Codec<CookingBookCategory> Codec = Codecs.String.ComapFlatMap(
        name => Parse(name) is { } category
            ? DataResult<CookingBookCategory>.Success(category)
            : DataResult<CookingBookCategory>.Error(() => $"Unknown cooking book category: {name}"),
        category => category.SerializedName);

    private CookingBookCategory(string serializedName, int id)
    {
        SerializedName = serializedName;
        Id = id;
    }

    public string SerializedName { get; }

    public int Id { get; }

    //ById looks up by the numeric id, out of range falls back to misc, maps to vanilla BY_ID
    public static CookingBookCategory ById(int id) => id is >= 0 and < 3 ? Values[id] : Misc;

    private static CookingBookCategory? Parse(string name)
    {
        foreach (var value in Values)
            if (value.SerializedName == name) return value;
        return null;
    }

    public override string ToString() => SerializedName;
}
