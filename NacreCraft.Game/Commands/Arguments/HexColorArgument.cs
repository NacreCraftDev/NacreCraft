using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//HexColorArgument hex color, maps to vanilla net.minecraft.commands.arguments.HexColorArgument
//Accepts RGB (3 digits) or RRGGBB (6 digits) and returns an opaque ARGB value
public sealed class HexColorArgument : ArgumentType<int>
{
    public static readonly DynamicCommandExceptionType ErrorInvalidHex =
        new(value => new TranslatableMessage("argument.hexcolor.invalid", value));

    private static readonly IReadOnlyList<string> ExampleList = new[] { "F00", "FF0000" };

    private HexColorArgument() { }

    public static HexColorArgument HexColor() => new();

    public int Parse(StringReader reader)
    {
        var text = reader.ReadUnquotedString();
        try
        {
            return (int)(text.Length switch
            {
                3 => 0xFF000000u | (uint)(DuplicateDigit(Convert.ToInt32(text[..1], 16)) << 16)
                    | (uint)(DuplicateDigit(Convert.ToInt32(text[1..2], 16)) << 8)
                    | (uint)DuplicateDigit(Convert.ToInt32(text[2..3], 16)),
                6 => 0xFF000000u | (uint)Convert.ToInt32(text, 16),
                _ => throw new FormatException(),
            });
        }
        catch (Exception e) when (e is FormatException or ArgumentOutOfRangeException)
        {
            throw ErrorInvalidHex.CreateWithContext(reader, text);
        }
    }

    public IReadOnlyList<string> Examples => ExampleList;

    private static int DuplicateDigit(int digit) => digit * 17;
}
