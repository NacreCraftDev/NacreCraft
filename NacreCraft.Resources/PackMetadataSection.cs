using NacreCraft.Codec;
using NacreCraft.Util;

namespace NacreCraft.Resources;

//PackMetadataSection pack metadata section, maps to vanilla net.minecraft.server.packs.metadata.pack.PackMetadataSection
//Holds the pack description plus the range of pack formats the pack declares support for
public sealed record PackMetadataSection(string Description, InclusiveRange<PackFormat> SupportedFormats)
{
    //SectionName is the metadata section name inside pack.mcmeta, maps to vanilla pack
    public const string SectionName = "pack";

    //FullCodec reads description and supported_formats; vanilla builds a separate instance per pack type because
    //the two disagree on the last pre-minor version, a distinction NC does not need until it checks game compatibility
    private static readonly Codec<PackMetadataSection> FullCodec = RecordCodecBuilder.Of2(
        Codecs.String.FieldOf("description").ForGetter<PackMetadataSection, string>(s => s.Description),
        InclusiveRange<PackFormat>.Codec(PackFormat.Codec).FieldOf("supported_formats")
            .ForGetter<PackMetadataSection, InclusiveRange<PackFormat>>(s => s.SupportedFormats),
        (description, formats) => new PackMetadataSection(description, formats));

    //FallbackCodec reads the description alone, for callers that do not know the pack type yet
    private static readonly Codec<PackMetadataSection> FallbackCodec = RecordCodecBuilder.Of1(
        Codecs.String.FieldOf("description").ForGetter<PackMetadataSection, string>(s => s.Description),
        description => new PackMetadataSection(description,
            new InclusiveRange<PackFormat>(PackFormat.Of(PackFormat.MaxMinor))));

    public static readonly MetadataSectionType<PackMetadataSection> ClientType = new(SectionName, FullCodec);

    public static readonly MetadataSectionType<PackMetadataSection> ServerType = new(SectionName, FullCodec);

    public static readonly MetadataSectionType<PackMetadataSection> FallbackType = new(SectionName, FallbackCodec);

    //ForPackType picks the section type for a pack type, maps to vanilla PackMetadataSection.forPackType
    public static MetadataSectionType<PackMetadataSection> ForPackType(PackType packType)
        => packType == PackType.ClientResources ? ClientType : ServerType;
}
