using System.Text.RegularExpressions;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//UuidArgument uuid, maps to vanilla net.minecraft.commands.arguments.UuidArgument
public sealed partial class UuidArgument : ArgumentType<Guid>
{
    public static readonly SimpleCommandExceptionType ErrorInvalidUuid =
        new(new TranslatableMessage("argument.uuid.invalid"));

    private static readonly IReadOnlyList<string> ExampleList =
        new[] { "dd12be42-52a9-4a91-a8a1-11c01849e498" };

    [GeneratedRegex("^([-A-Fa-f0-9]+)")]
    private static partial Regex AllowedCharacters();

    public static UuidArgument Uuid() => new();

    public Guid Parse(StringReader reader)
    {
        var match = AllowedCharacters().Match(reader.Remaining);
        if (match.Success && Guid.TryParse(match.Groups[1].Value, out var result))
        {
            for (var i = 0; i < match.Groups[1].Value.Length; i++) reader.Skip();
            return result;
        }
        throw ErrorInvalidUuid.CreateWithContext(reader);
    }

    public IReadOnlyList<string> Examples => ExampleList;
}
