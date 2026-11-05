using NacreCraft.Codec;
using NacreCraft.Registry.Flag;

namespace NacreCraft.Resources;

//FeatureFlagsMetadataSection the pack's enabled feature flags, maps to vanilla net.minecraft.server.packs.FeatureFlagsMetadataSection
public sealed record FeatureFlagsMetadataSection(FeatureFlagSet Flags)
{
    private static readonly Codec<FeatureFlagsMetadataSection> SectionCodec = RecordCodecBuilder.Of1(
        FeatureFlags.CODEC.FieldOf("enabled").ForGetter((FeatureFlagsMetadataSection section) => section.Flags),
        flags => new FeatureFlagsMetadataSection(flags));

    public static readonly MetadataSectionType<FeatureFlagsMetadataSection> TYPE = new("features", SectionCodec);
}
