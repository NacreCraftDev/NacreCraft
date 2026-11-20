using NacreCraft.Network.Chat;

namespace NacreCraft.Client;

//TextureFilteringMethod texture filtering preference, maps to vanilla net.minecraft.client.TextureFilteringMethod
public enum TextureFilteringMethod
{
    None,
    Rgss,
    Anisotropic
}

public static class TextureFilteringMethodExtensions
{
    //Id the numeric id persisted in options, maps to vanilla id
    public static int Id(this TextureFilteringMethod value)
        => value switch
        {
            TextureFilteringMethod.None => 0,
            TextureFilteringMethod.Rgss => 1,
            _ => 2
        };

    //Caption the translated label shown in the options screen
    public static Component Caption(this TextureFilteringMethod value)
        => Component.Translatable(value switch
        {
            TextureFilteringMethod.None => "options.textureFiltering.none",
            TextureFilteringMethod.Rgss => "options.textureFiltering.rgss",
            _ => "options.textureFiltering.anisotropic"
        });
}
