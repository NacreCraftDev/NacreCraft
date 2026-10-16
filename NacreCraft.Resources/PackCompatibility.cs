using NacreCraft.Util;

namespace NacreCraft.Resources;

//PackCompatibility how a pack's declared format range sits against the running game version
//maps to vanilla net.minecraft.server.packs.repository.PackCompatibility
public enum PackCompatibility
{
    TooOld,
    TooNew,
    Unknown,
    Compatible
}

//PackCompatibilityRules holds the range check, kept in the same file as the enum it operates on
public static class PackCompatibilityRules
{
    //The running pack formats, taken from vanilla DetectedVersion where CLIENT_RESOURCES is 88.0 and SERVER_DATA is 107.1
    public static readonly PackFormat ClientPackFormat = PackFormat.Of(88, 0);
    public static readonly PackFormat ServerPackFormat = PackFormat.Of(107, 1);

    //GameFormat the format the given pack type is checked against
    public static PackFormat GameFormat(PackType type)
        => type == PackType.ClientResources ? ClientPackFormat : ServerPackFormat;

    //ForVersion classifies a declared format range, maps to vanilla PackCompatibility.forVersion
    //A range whose minimum major is MaxMinor is what the fallback codec produces, meaning the pack never declared a version
    public static PackCompatibility ForVersion(InclusiveRange<PackFormat> declared, PackFormat gameSupported)
    {
        if (declared.MinInclusive.Major == int.MaxValue) return PackCompatibility.Unknown;
        if (declared.MaxInclusive.CompareTo(gameSupported) < 0) return PackCompatibility.TooOld;
        if (gameSupported.CompareTo(declared.MinInclusive) < 0) return PackCompatibility.TooNew;
        return PackCompatibility.Compatible;
    }

    //IsCompatible only a pack declaring support for the running version counts, maps to vanilla isCompatible
    public static bool IsCompatible(this PackCompatibility compatibility)
        => compatibility == PackCompatibility.Compatible;
}
