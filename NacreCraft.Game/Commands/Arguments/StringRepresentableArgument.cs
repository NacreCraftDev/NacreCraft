using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Commands.Context;
using NacreCraft.Commands;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//StringRepresentableArgument enum-by-name argument, maps to vanilla net.minecraft.commands.arguments.StringRepresentableArgument
//Subclasses supply the value list, the serialized name and the name parser
public abstract class StringRepresentableArgument<T> : ArgumentType<T> where T : struct
{
    private static readonly DynamicCommandExceptionType ErrorInvalidValue =
        new(value => new TranslatableMessage("argument.enum.invalid", value));

    private readonly Func<IReadOnlyList<T>> _values;
    private readonly Func<T, string> _serializedName;
    private readonly Func<string, T?> _parser;

    protected StringRepresentableArgument(Func<IReadOnlyList<T>> values, Func<T, string> serializedName,
        Func<string, T?> parser)
    {
        _values = values;
        _serializedName = serializedName;
        _parser = parser;
    }

    public T Parse(StringReader reader)
    {
        var id = reader.ReadUnquotedString();
        return _parser(ConvertId(id)) ?? throw ErrorInvalidValue.CreateWithContext(reader, id);
    }

    public Task<Suggestions> ListSuggestions<S>(CommandContext<S> context, SuggestionsBuilder builder)
    {
        foreach (var value in _values()) builder.Add(ConvertId(_serializedName(value)));
        return builder.BuildFuture();
    }

    public IReadOnlyList<string> Examples
    {
        get
        {
            var examples = new List<string>(2);
            foreach (var value in _values())
            {
                examples.Add(ConvertId(_serializedName(value)));
                if (examples.Count == 2) break;
            }
            return examples;
        }
    }

    protected virtual string ConvertId(string id) => id;
}
