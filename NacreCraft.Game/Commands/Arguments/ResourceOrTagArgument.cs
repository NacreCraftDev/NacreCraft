using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands;
using NacreCraft.Registry;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ResourceOrTagArgument registry element or tag, maps to vanilla net.minecraft.commands.arguments.ResourceOrTagArgument
//A # prefix selects a tag set, otherwise a single holder is resolved from the registry
public sealed class ResourceOrTagArgument<T> : ArgumentType<ResourceOrTagArgument<T>.Result> where T : class
{
    public static readonly Dynamic2CommandExceptionType ErrorUnknownElement =
        new((value, registry) => new TranslatableMessage("argument.resource.not_found", value, registry));

    private readonly Registry<T> _registry;

    public ResourceOrTagArgument(Registry<T> registry) => _registry = registry;

    public Result Parse(StringReader reader)
    {
        var start = reader.Cursor;
        var isTag = reader.CanRead() && reader.Peek() == '#';
        if (isTag) reader.Skip();
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        var text = reader.String[(start + (isTag ? 1 : 0))..reader.Cursor];
        var id = Identifier.Parse(text);
        if (isTag) return new Result(null, _registry.GetOrCreate(TagKey<T>.Create(_registry.Key, id)));
        var holder = _registry.GetHolder(id);
        return holder is not null
            ? new Result(holder, null)
            : throw ErrorUnknownElement.CreateWithContext(reader, text, _registry.Key.Identifier.ToString());
    }

    public sealed record Result(Holder<T>? Element, HolderSet<T>? Tag);
}
