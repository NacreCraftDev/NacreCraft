using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Game.Server;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//DimensionArgument dimension argument, maps to vanilla net.minecraft.commands.arguments.DimensionArgument
//Parses the dimension identifier; at execution time it fetches the instance from the server's built dimension table and throws unknown dimension like vanilla when not found
public sealed class DimensionArgument : ArgumentType<Identifier>
{
    private static readonly IReadOnlyList<string> ExamplesList =
        new[] { "overworld", "overworld:the_nether", "minecraft:the_end" };

    public static readonly DynamicCommandExceptionType ErrorUnknownDimension =
        new(id => new LiteralMessage($"unknown dimension {id}"));

    public static DimensionArgument Dimension() => new();

    public Identifier Parse(StringReader reader) => IdentifierArgument.ReadIdentifier(reader);

    //GetDimension gets the dimension world, maps to vanilla DimensionArgument.getDimension
    public static PersistentServerLevel GetDimension(CommandContext<CommandSourceStack> context, string name)
    {
        var id = context.GetArgument<Identifier>(name);
        if (context.GetSource() is not ServerCommandSource source) throw ErrorUnknownDimension.Create(id);
        var key = ResourceKey<NacreCraft.Registry.Level>.Create(Registries.DIMENSION, id);
        return source.Server.GetLevel(key) ?? throw ErrorUnknownDimension.Create(id);
    }

    public IReadOnlyList<string> Examples => ExamplesList;
}
