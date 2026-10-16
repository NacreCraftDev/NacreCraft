namespace NacreCraft.Resources;

//PackSource where a pack came from, maps to vanilla net.minecraft.server.packs.repository.PackSource
//ShouldAddAutomatically packs join the selection without the user enabling them, which is how all sources but Feature behave
//TranslationKey names the description suffix the pack screen appends; null means the source stays unlabelled
public sealed record PackSource(string Id, bool ShouldAddAutomatically, string? TranslationKey)
{
    public static readonly PackSource Default = new("default", true, null);
    public static readonly PackSource BuiltIn = new("built_in", true, "pack.source.builtin");
    public static readonly PackSource Feature = new("feature", false, "pack.source.feature");
    public static readonly PackSource World = new("world", true, "pack.source.world");
    public static readonly PackSource Server = new("server", true, "pack.source.server");
}
