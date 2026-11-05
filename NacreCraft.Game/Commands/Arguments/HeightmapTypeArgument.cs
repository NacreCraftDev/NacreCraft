using NacreCraft.Registry;

namespace NacreCraft.Game.Commands.Arguments;

//HeightmapTypeArgument heightmap type, maps to vanilla net.minecraft.commands.arguments.HeightmapTypeArgument
//Names are matched case-insensitively; the worldgen-only types are not filtered out yet
public sealed class HeightmapTypeArgument : StringRepresentableArgument<Heightmap.Types>
{
    private static readonly Heightmap.Types[] Values = Enum.GetValues<Heightmap.Types>();

    private HeightmapTypeArgument()
        : base(() => Values, SerializedName, ParseName)
    {
    }

    public static HeightmapTypeArgument Heightmap() => new();

    private static string SerializedName(Heightmap.Types type) => type.ToString().ToLowerInvariant();

    private static Heightmap.Types? ParseName(string name)
    {
        foreach (var type in Values)
            if (string.Equals(type.ToString(), name, StringComparison.OrdinalIgnoreCase)) return type;
        return null;
    }
}
