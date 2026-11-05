using NacreCraft.Commands.Arguments;
using NacreCraft.Registry;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ResourceOrTagKeyArgument element key or tag key, maps to vanilla net.minecraft.commands.arguments.ResourceOrTagKeyArgument
//A # prefix selects a tag key, otherwise the token is an element key
public sealed class ResourceOrTagKeyArgument<T> : ArgumentType<ResourceOrTagKeyArgument<T>.Result> where T : class
{
    private readonly Registry<T> _registry;

    public ResourceOrTagKeyArgument(Registry<T> registry) => _registry = registry;

    public Result Parse(StringReader reader)
    {
        var start = reader.Cursor;
        var isTag = reader.CanRead() && reader.Peek() == '#';
        if (isTag) reader.Skip();
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        var id = Identifier.Parse(reader.String[(start + (isTag ? 1 : 0))..reader.Cursor]);
        return isTag
            ? new Result(null, TagKey<T>.Create(_registry.Key, id))
            : new Result(ResourceKey<T>.Create(_registry.Key, id), null);
    }

    public sealed record Result(ResourceKey<T>? Key, TagKey<T>? Tag);
}
