using NacreCraft.Commands.Arguments;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//UnresolvedRegistryArgument client-side fallback for resource_or_tag/resource_or_tag_key
//The server writes the registry key but not the element CLR type, so the client parses the token as a plain string
public sealed class UnresolvedRegistryArgument : ArgumentType<string>, IRegistryKeyedArgument
{
    public UnresolvedRegistryArgument(NacreCraft.Registry.Identifier registryIdentifier)
        => RegistryIdentifier = registryIdentifier;

    public NacreCraft.Registry.Identifier RegistryIdentifier { get; }

    public string Parse(StringReader reader) => reader.ReadUnquotedString();

    public IReadOnlyList<string> Examples => Array.Empty<string>();
}
