namespace NacreCraft.Client.Render.Texture;

//MipmapStrategy how mip levels are produced for one texture, maps to vanilla MipmapStrategy
public enum MipmapStrategy
{
    Auto,
    Mean,
    Cutout,
    StrictCutout,
    DarkCutout
}

public static class MipmapStrategyExtensions
{
    //GetSerializedName returns the .mcmeta spelling of the strategy
    public static string GetSerializedName(this MipmapStrategy strategy) => strategy switch
    {
        MipmapStrategy.Mean => "mean",
        MipmapStrategy.Cutout => "cutout",
        MipmapStrategy.StrictCutout => "strict_cutout",
        MipmapStrategy.DarkCutout => "dark_cutout",
        _ => "auto"
    };

    //TryParse reads the .mcmeta spelling, unknown names fall back to Auto like vanilla's optional field
    public static MipmapStrategy Parse(string? name) => name switch
    {
        "mean" => MipmapStrategy.Mean,
        "cutout" => MipmapStrategy.Cutout,
        "strict_cutout" => MipmapStrategy.StrictCutout,
        "dark_cutout" => MipmapStrategy.DarkCutout,
        _ => MipmapStrategy.Auto
    };
}
