namespace NacreCraft.Resources;

//KnownPack a pack the server knows by name and version, maps to vanilla net.minecraft.server.packs.repository.KnownPack
public sealed record KnownPack(string Namespace, string Id, string Version)
{
    public const string VANILLA_NAMESPACE = "minecraft";

    public bool IsVanilla() => Namespace == VANILLA_NAMESPACE;

    public override string ToString() => $"{Namespace}:{Id}:{Version}";
}
