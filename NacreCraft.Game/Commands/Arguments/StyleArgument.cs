using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands;
using NacreCraft.Network.Chat;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//StyleArgument chat style SNBT object, maps to vanilla net.minecraft.commands.arguments.StyleArgument
//The style serializer is not ported, so the object text is consumed and an empty style is returned
public sealed class StyleArgument : ArgumentType<Style>
{
    public static readonly DynamicCommandExceptionType ErrorInvalidStyle =
        new(message => new TranslatableMessage("argument.style.invalid", message));

    private static readonly IReadOnlyList<string> ExampleList =
        new[] { "{bold: true}", "{color: 'red'}", "{}" };

    private StyleArgument() { }

    public static StyleArgument StyleArg() => new();

    public Style Parse(StringReader reader)
    {
        if (!reader.CanRead() || reader.Peek() != '{')
            throw ErrorInvalidStyle.CreateWithContext(reader, "expected an object");
        var depth = 0;
        while (reader.CanRead())
        {
            var c = reader.Peek();
            reader.Skip();
            if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return Style.Empty;
        }
        throw ErrorInvalidStyle.CreateWithContext(reader, "unclosed object");
    }

    public IReadOnlyList<string> Examples => ExampleList;
}
