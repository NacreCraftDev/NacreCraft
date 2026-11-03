using NacreCraft.Codec;

namespace NacreCraft.Resources;

//ResourceFilterSection the pack.mcmeta "filter" section, maps to vanilla net.minecraft.server.packs.resources.ResourceFilterSection
//Lists which namespaces and paths a pack refuses to serve, so a lower pack keeps answering for them
public sealed class ResourceFilterSection
{
    private static readonly Codec<ResourceFilterSection> SectionCodec = RecordCodecBuilder.Of1(
        IdentifierPattern.PatternCodec.ListOf().FieldOf("block")
            .ForGetter<ResourceFilterSection, IReadOnlyList<IdentifierPattern>>(s => s._blockList),
        blockList => new ResourceFilterSection(blockList));

    //Type the metadata section descriptor, maps to vanilla ResourceFilterSection.TYPE
    public static readonly MetadataSectionType<ResourceFilterSection> Type = new("filter", SectionCodec);

    private readonly IReadOnlyList<IdentifierPattern> _blockList;

    public ResourceFilterSection(IReadOnlyList<IdentifierPattern> blockList) => _blockList = blockList.ToList();

    public bool IsNamespaceFiltered(string namespaceName) => _blockList.Any(p => p.MatchesNamespace(namespaceName));

    public bool IsPathFiltered(string path) => _blockList.Any(p => p.MatchesPath(path));
}
