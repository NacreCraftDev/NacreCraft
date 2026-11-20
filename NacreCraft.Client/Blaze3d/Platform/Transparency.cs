namespace NacreCraft.Client.Blaze3d.Platform;

//Transparency which kinds of transparent pixels an image contains, maps to vanilla com.mojang.blaze3d.platform.Transparency
public readonly record struct Transparency(bool HasTransparent, bool HasTranslucent)
{
    public static readonly Transparency None = new(false, false);
    public static readonly Transparency Transparent = new(true, false);
    public static readonly Transparency Translucent = new(false, true);
    public static readonly Transparency TransparentAndTranslucent = new(true, true);

    public static Transparency Of(bool hasTransparent, bool hasTranslucent)
        => new(hasTransparent, hasTranslucent);

    public Transparency Or(Transparency other)
        => Of(HasTransparent || other.HasTransparent, HasTranslucent || other.HasTranslucent);

    public bool IsOpaque() => !HasTransparent && !HasTranslucent;
}
