using System.Text.RegularExpressions;
using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Resources;

//IdentifierPattern a namespace/path regex pair used by pack filters, maps to vanilla net.minecraft.util.IdentifierPattern
//A missing pattern matches everything, matching vanilla's default predicate
public sealed class IdentifierPattern
{
    public static readonly Codec<IdentifierPattern> PatternCodec = RecordCodecBuilder.Of2(
        Codecs.String.OptionalFieldOf("namespace").ForGetter<IdentifierPattern, Optional<string>>(p => p._namespaceSource),
        Codecs.String.OptionalFieldOf("path").ForGetter<IdentifierPattern, Optional<string>>(p => p._pathSource),
        (namespacePattern, pathPattern) => new IdentifierPattern(namespacePattern, pathPattern));

    private readonly Optional<string> _namespaceSource;
    private readonly Optional<string> _pathSource;
    private readonly Regex? _namespaceRegex;
    private readonly Regex? _pathRegex;

    private IdentifierPattern(Optional<string> namespacePattern, Optional<string> pathPattern)
    {
        _namespaceSource = namespacePattern;
        _pathSource = pathPattern;
        _namespaceRegex = namespacePattern.IsPresent ? new Regex(namespacePattern.Get()) : null;
        _pathRegex = pathPattern.IsPresent ? new Regex(pathPattern.Get()) : null;
    }

    public bool MatchesNamespace(string namespaceName) => _namespaceRegex?.IsMatch(namespaceName) ?? true;

    public bool MatchesPath(string path) => _pathRegex?.IsMatch(path) ?? true;

    public bool Matches(Identifier location) => MatchesNamespace(location.Namespace) && MatchesPath(location.Path);
}
