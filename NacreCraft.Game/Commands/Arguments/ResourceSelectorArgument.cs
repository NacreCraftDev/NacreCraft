using System.Text;
using System.Text.RegularExpressions;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands;
using NacreCraft.Registry;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ResourceSelectorArgument registry wildcard selector, maps to vanilla net.minecraft.commands.arguments.ResourceSelectorArgument
//A pattern such as minecraft:* selects every matching element of the registry
public sealed class ResourceSelectorArgument<T> : ArgumentType<IReadOnlyList<Holder<T>>> where T : class
{
    public static readonly Dynamic2CommandExceptionType ErrorNoMatches =
        new((selector, registry) => new TranslatableMessage("argument.resource_selector.not_found", selector, registry));

    private readonly Registry<T> _registry;

    private ResourceSelectorArgument(Registry<T> registry) => _registry = registry;

    public static ResourceSelectorArgument<T> ResourceSelector(Registry<T> registry) => new(registry);

    public IReadOnlyList<Holder<T>> Parse(StringReader reader)
    {
        var pattern = EnsureNamespaced(ReadPattern(reader));
        var regex = new Regex(ToRegex(pattern));
        var results = new List<Holder<T>>();
        foreach (var holder in _registry.ListElements())
        {
            if (holder.UnwrapKey() is { } resourceKey && regex.IsMatch(resourceKey.Identifier.ToString()))
                results.Add(holder);
        }
        if (results.Count == 0)
            throw ErrorNoMatches.CreateWithContext(reader, pattern, _registry.Key.Identifier.ToString());
        return results;
    }

    private static string ReadPattern(StringReader reader)
    {
        var builder = new StringBuilder();
        while (reader.CanRead() && IsAllowedPatternCharacter(reader.Peek()))
        {
            builder.Append(reader.Peek());
            reader.Skip();
        }
        return builder.ToString();
    }

    private static bool IsAllowedPatternCharacter(char c)
        => char.IsLetterOrDigit(c) || c is '_' or '-' or '.' or ':' or '*' or '?';

    private static string EnsureNamespaced(string input)
        => input.Contains(':') ? input : "minecraft:" + input;

    //ToRegex converts the * and ? wildcards into a regex; everything else is quoted
    private static string ToRegex(string pattern)
    {
        var builder = new StringBuilder("^");
        foreach (var c in pattern)
        {
            builder.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                _ => Regex.Escape(c.ToString()),
            });
        }
        builder.Append('$');
        return builder.ToString();
    }
}
