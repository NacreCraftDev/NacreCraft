using NacreCraft.Commands.Arguments;
using NacreCraft.Registry;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ResourceOrIdArgument resolved holder or plain id, maps to vanilla net.minecraft.commands.arguments.ResourceOrIdArgument
//Used where an element may not be loaded yet: a missing element is kept as its id
public sealed class ResourceOrIdArgument<T> : ArgumentType<ResourceOrIdArgument<T>.Result> where T : class
{
    private readonly Registry<T> _registry;

    public ResourceOrIdArgument(Registry<T> registry) => _registry = registry;

    public Result Parse(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        var id = Identifier.Parse(reader.String[start..reader.Cursor]);
        var holder = _registry.GetHolder(id);
        return holder is not null ? new Result(holder, null) : new Result(null, id);
    }

    public sealed record Result(Holder<T>? Element, Identifier? Id);
}
