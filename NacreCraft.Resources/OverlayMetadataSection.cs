using NacreCraft.Codec;
using NacreCraft.Util;

namespace NacreCraft.Resources;

//OverlayMetadataSection the overlays declared by a pack, maps to vanilla net.minecraft.server.packs.OverlayMetadataSection
//The simplified entry form takes an explicit formats range; the compact single-number form of vanilla is not parsed yet
public sealed record OverlayMetadataSection(IReadOnlyList<OverlayMetadataSection.OverlayEntry> Overlays)
{
    public static readonly MetadataSectionType<OverlayMetadataSection> CLIENT_TYPE =
        new("overlays", CodecForPackType(PackType.ClientResources));

    public static readonly MetadataSectionType<OverlayMetadataSection> SERVER_TYPE =
        new("overlays", CodecForPackType(PackType.ServerData));

    public static Codec<OverlayMetadataSection> CodecForPackType(PackType packType)
        => RecordCodecBuilder.Of1(
            OverlayEntry.Codec.ListOf().FieldOf("entries")
                .ForGetter((OverlayMetadataSection section) => section.Overlays),
            entries => new OverlayMetadataSection(entries));

    public static MetadataSectionType<OverlayMetadataSection> ForPackType(PackType packType)
        => packType == PackType.ClientResources ? CLIENT_TYPE : SERVER_TYPE;

    //OverlaysForVersion the overlay directories applicable to the given pack format, maps to vanilla overlaysForVersion
    public IReadOnlyList<string> OverlaysForVersion(PackFormat version)
        => Overlays.Where(entry => entry.IsApplicable(version)).Select(entry => entry.Overlay).ToList();

    public sealed record OverlayEntry(InclusiveRange<PackFormat> Format, string Overlay)
    {
        public static readonly Codec<OverlayEntry> Codec = RecordCodecBuilder.Of2(
            InclusiveRange<PackFormat>.Codec(PackFormat.Codec).FieldOf("formats")
                .ForGetter((OverlayEntry entry) => entry.Format),
            Codecs.String.FieldOf("directory").ForGetter((OverlayEntry entry) => entry.Overlay),
            (format, overlay) => new OverlayEntry(format, overlay));

        public bool IsApplicable(PackFormat formatToTest) => Format.IsValueInRange(formatToTest);
    }
}
