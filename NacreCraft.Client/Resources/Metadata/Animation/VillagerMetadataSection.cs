using System.Text.Json;

namespace NacreCraft.Client.Resources.Metadata.Animation;

//VillagerMetadataSection parses the villager section of an entity texture .mcmeta, maps to vanilla VillagerMetadataSection
public sealed record VillagerMetadataSection(VillagerMetadataSection.Hat HatKind)
{
    public enum Hat
    {
        None,
        Partial,
        Full
    }

    public static readonly VillagerMetadataSection Default = new(Hat.None);

    //Parse returns the default section when the root has no villager section
    public static VillagerMetadataSection Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("villager", out var villager)
            || villager.ValueKind != JsonValueKind.Object)
            return Default;
        var name = villager.TryGetProperty("hat", out var hat) ? hat.GetString() : null;
        return new VillagerMetadataSection(name switch
        {
            "partial" => Hat.Partial,
            "full" => Hat.Full,
            _ => Hat.None
        });
    }
}
