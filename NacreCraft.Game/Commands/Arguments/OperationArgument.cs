using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//OperationArgument scoreboard operation, maps to vanilla net.minecraft.commands.arguments.OperationArgument
//Parses one of the assignment or arithmetic operator tokens
public sealed class OperationArgument : ArgumentType<OperationArgument.Operation>
{
    public static readonly SimpleCommandExceptionType ErrorInvalidOperation =
        new(new TranslatableMessage("arguments.operation.invalid"));

    private static readonly IReadOnlyList<string> ExampleList =
        new[] { "=", "+=", "-=", "*=", "/=", "<", ">", "><" };

    public static OperationArgument ScoreboardOperation() => new();

    public Operation Parse(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() is '=' or '+' or '-' or '*' or '/' or '<' or '>') reader.Skip();
        var token = reader.String[start..reader.Cursor];
        return token switch
        {
            "=" => new Operation(OperationType.Assign, false),
            "+=" => new Operation(OperationType.Add, false),
            "-=" => new Operation(OperationType.Subtract, false),
            "*=" => new Operation(OperationType.Multiply, false),
            "/=" => new Operation(OperationType.Divide, false),
            "<" => new Operation(OperationType.Min, false),
            ">" => new Operation(OperationType.Max, false),
            "><" => new Operation(OperationType.Swap, false),
            _ => throw ErrorInvalidOperation.CreateWithContext(reader),
        };
    }

    public IReadOnlyList<string> Examples => ExampleList;

    public enum OperationType
    {
        Assign,
        Add,
        Subtract,
        Multiply,
        Divide,
        Min,
        Max,
        Swap,
    }

    public sealed record Operation(OperationType Type, bool Extra);
}
