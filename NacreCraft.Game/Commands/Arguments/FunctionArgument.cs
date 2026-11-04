using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Functions;
using NacreCraft.Registry;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//FunctionArgument function argument, maps to vanilla net.minecraft.commands.arguments.item.FunctionArgument
//Syntax: <function identifier> or #<function tag>; the parse result resolves lazily because the function library reloads
//No command consumes it yet; it is registered so the function argument type can be written into the command tree instead of staying a placeholder
public sealed class FunctionArgument : ArgumentType<FunctionArgument.Result>
{
    private static readonly IReadOnlyList<string> ExamplesList = new[] { "foo", "foo:bar", "#foo" };

    public static readonly DynamicCommandExceptionType ErrorUnknownTag =
        new(tag => new LiteralMessage($"unknown function tag {tag}"));

    public static readonly DynamicCommandExceptionType ErrorUnknownFunction =
        new(value => new LiteralMessage($"unknown function {value}"));

    public static FunctionArgument Functions() => new();

    public Result Parse(StringReader reader)
    {
        if (reader.CanRead() && reader.Peek() == '#')
        {
            reader.Skip();
            return new TagResult(IdentifierArgument.ReadIdentifier(reader));
        }
        return new FunctionResult(IdentifierArgument.ReadIdentifier(reader));
    }

    private static CommandFunction<CommandSourceStack> GetFunction(CommandContext<CommandSourceStack> context,
        Identifier id)
        => ServerOf(context).Functions.Get(id)
           ?? throw ErrorUnknownFunction.Create(id.ToString());

    private static IReadOnlyList<CommandFunction<CommandSourceStack>> GetFunctionTag(
        CommandContext<CommandSourceStack> context, Identifier id)
        => ServerOf(context).Functions.Library.GetTags().TryGetValue(id, out var tag)
            ? tag
            : throw ErrorUnknownTag.Create(id.ToString());

    //ServerOf the server behind a command source; only the server source carries one
    private static NacreCraft.Game.Server.MinecraftServer ServerOf(CommandContext<CommandSourceStack> context)
        => context.GetSource() is ServerCommandSource source
            ? source.Server
            : throw new InvalidOperationException("function argument needs a server command source");

    //GetFunctions resolves the argument into the functions it names, maps to vanilla getFunctions
    public static IReadOnlyCollection<CommandFunction<CommandSourceStack>> GetFunctions(
        CommandContext<CommandSourceStack> context, string name)
        => context.GetArgument<Result>(name).Create(context);

    //GetFunctionCollection resolves the argument into its identifier plus the named functions, maps to vanilla getFunctionCollection
    public static (Identifier Id, IReadOnlyList<CommandFunction<CommandSourceStack>> Functions) GetFunctionCollection(
        CommandContext<CommandSourceStack> context, string name)
        => context.GetArgument<Result>(name).UnwrapToCollection(context);

    public IReadOnlyList<string> Examples => ExamplesList;

    //Result a parsed function reference, maps to vanilla FunctionArgument.Result
    public interface Result
    {
        Identifier Id { get; }

        bool IsTag { get; }

        IReadOnlyCollection<CommandFunction<CommandSourceStack>> Create(CommandContext<CommandSourceStack> context);

        (Identifier Id, IReadOnlyList<CommandFunction<CommandSourceStack>> Functions) UnwrapToCollection(
            CommandContext<CommandSourceStack> context);
    }

    //FunctionResult a single function reference
    private sealed class FunctionResult(Identifier id) : Result
    {
        public Identifier Id { get; } = id;

        public bool IsTag => false;

        public IReadOnlyCollection<CommandFunction<CommandSourceStack>> Create(
            CommandContext<CommandSourceStack> context)
            => new[] { GetFunction(context, Id) };

        public (Identifier Id, IReadOnlyList<CommandFunction<CommandSourceStack>> Functions) UnwrapToCollection(
            CommandContext<CommandSourceStack> context)
            => (Id, new[] { GetFunction(context, Id) });
    }

    //TagResult a function tag reference
    private sealed class TagResult(Identifier id) : Result
    {
        public Identifier Id { get; } = id;

        public bool IsTag => true;

        public IReadOnlyCollection<CommandFunction<CommandSourceStack>> Create(
            CommandContext<CommandSourceStack> context)
            => GetFunctionTag(context, Id);

        public (Identifier Id, IReadOnlyList<CommandFunction<CommandSourceStack>> Functions) UnwrapToCollection(
            CommandContext<CommandSourceStack> context)
            => (Id, GetFunctionTag(context, Id));
    }
}
