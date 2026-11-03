using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Primitives;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//SwizzleArgument axis swizzle argument, maps to vanilla net.minecraft.commands.arguments.coordinates.SwizzleArgument
//Reads x/y/z characters into an axis set; an unknown or repeated character is rejected
public sealed class SwizzleArgument : ArgumentType<HashSet<Direction.Axis>>
{
    public static readonly SimpleCommandExceptionType ErrorInvalid =
        new(new TranslatableMessage("arguments.swizzle.invalid"));

    public static SwizzleArgument Swizzle() => new();

    public HashSet<Direction.Axis> Parse(StringReader reader)
    {
        var result = new HashSet<Direction.Axis>();
        while (reader.CanRead() && reader.Peek() != ' ')
        {
            var axis = reader.Read() switch
            {
                'x' => Direction.Axis.X,
                'y' => Direction.Axis.Y,
                'z' => Direction.Axis.Z,
                _ => throw ErrorInvalid.CreateWithContext(reader)
            };
            if (!result.Add(axis)) throw ErrorInvalid.CreateWithContext(reader);
        }
        return result;
    }

    public static HashSet<Direction.Axis> GetSwizzle(CommandContext<CommandSourceStack> context, string name)
        => context.GetArgument<HashSet<Direction.Axis>>(name);

    public IReadOnlyList<string> Examples => new[] { "xyz", "x" };
}
