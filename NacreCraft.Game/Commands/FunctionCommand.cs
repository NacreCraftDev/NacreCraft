using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Execution;
using NacreCraft.Commands.Functions;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Commands.Data;
using NacreCraft.Game.Server;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.Commands;

//FunctionCommand function command, maps to vanilla net.minecraft.server.commands.FunctionCommand
//The function library and execution queue already exist, so functions and tags are really executed; macro arguments come
//from an inline compound tag or the with <block|entity|storage> [<path>] branch backed by the data command accessors
public static class FunctionCommand
{
    //ErrorNoFunctions the resolved tag or function set is empty, maps to vanilla commands.function.scheduled.no_functions
    private static readonly DynamicCommandExceptionType ErrorNoFunctions =
        new(name => new LiteralMessage($"no functions were found for {name}"));

    //ErrorArgumentNotCompound the with path points at something other than a single compound tag
    private static readonly DynamicCommandExceptionType ErrorArgumentNotCompound =
        new(type => new LiteralMessage($"the argument must be a compound tag, got {type}"));

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var with = LiteralArgumentBuilder<CommandSourceStack>.Literal("with");
        AddBlockProvider(with);
        AddEntityProvider(with);
        AddStorageProvider(with);

        var arguments = Arg<CompoundTag>("arguments", CompoundTagArgument.CompoundTag())
            .Executes(context => Execute(context, CompoundTagArgument.GetCompoundTag(context, "arguments")));

        var name = Arg<FunctionArgument.Result>("name", FunctionArgument.Functions())
            .Executes(context => Execute(context, null))
            .Then(arguments)
            .Then(with);

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("function")
            .Requires(s => s.HasPermission(2))
            .Then(name));
    }

    //AddBlockProvider with block <sourcePos> [<path>], reads a block entity through the data accessor
    private static void AddBlockProvider(LiteralArgumentBuilder<CommandSourceStack> with)
    {
        var path = Arg<NbtPath>("path", NbtPathArgument.NbtPathArg())
            .Executes(context => ExecuteWithPath(context, BlockAccessor(context)));
        var locator = Arg<BlockCoordinates>("sourcePos", BlockPosArgument.BlockPos())
            .Executes(context => ExecuteWith(context, BlockAccessor(context)))
            .Then(path);
        with.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("block").Then(locator));
    }

    //AddEntityProvider with entity <source> [<path>], reads an entity through the data accessor
    private static void AddEntityProvider(LiteralArgumentBuilder<CommandSourceStack> with)
    {
        var path = Arg<NbtPath>("path", NbtPathArgument.NbtPathArg())
            .Executes(context => ExecuteWithPath(context, EntityAccessor(context)));
        var locator = Arg<EntitySelector>("source", EntityArgument.Entity())
            .Executes(context => ExecuteWith(context, EntityAccessor(context)))
            .Then(path);
        with.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("entity").Then(locator));
    }

    //AddStorageProvider with storage <source> [<path>], reads command storage
    private static void AddStorageProvider(LiteralArgumentBuilder<CommandSourceStack> with)
    {
        var path = Arg<NbtPath>("path", NbtPathArgument.NbtPathArg())
            .Executes(context => ExecuteWithPath(context, StorageAccessor(context)));
        var locator = Arg<Identifier>("source", IdentifierArgument.Id())
            .Executes(context => ExecuteWith(context, StorageAccessor(context)))
            .Then(path);
        with.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("storage").Then(locator));
    }

    private static IDataAccessor BlockAccessor(CommandContext<CommandSourceStack> context)
    {
        var source = (ServerCommandSource)context.GetSource();
        var pos = BlockPosArgument.GetBlockPos(context, "sourcePos");
        if (source.Server.BlockEntities.Get(pos) is not { } entity) throw BlockDataAccessor.ErrorNotBlockEntity.Create();
        if (source.Level is not PersistentServerLevel level) throw BlockDataAccessor.ErrorNotBlockEntity.Create();
        return new BlockDataAccessor(level, source.Server.PlayerList, entity, pos);
    }

    private static IDataAccessor EntityAccessor(CommandContext<CommandSourceStack> context)
        => new EntityDataAccessor(EntityArgument.GetSingleTarget(context, "source"));

    private static IDataAccessor StorageAccessor(CommandContext<CommandSourceStack> context)
        => new StorageDataAccessor(((ServerCommandSource)context.GetSource()).Server.CommandStorage,
            IdentifierArgument.GetId(context, "source"));

    //Execute runs every resolved function with the given macro arguments, maps to vanilla queueFunctions
    private static int Execute(CommandContext<CommandSourceStack> context, CompoundTag? arguments)
    {
        var source = (ServerCommandSource)context.GetSource();
        var (id, functions) = FunctionArgument.GetFunctionCollection(context, "name");
        if (functions.Count == 0) throw ErrorNoFunctions.Create(id.ToString());

        var executed = 0;
        foreach (var function in functions)
        {
            try
            {
                var instantiated = function.Instantiate(arguments, source.Server.Commands.Dispatcher);
                source.Server.Commands.ExecuteInContext(source, execution =>
                    ExecutionContext<CommandSourceStack>.QueueInitialFunctionCall(execution, instantiated, source,
                        CommandResultCallback.Empty));
                executed++;
            }
            catch (FunctionInstantiationException e)
            {
                source.SendFailure($"failed to instantiate function {function.Id}: {e.Message}");
            }
        }

        source.SendSuccess(executed == 1
            ? $"ran function {functions.First().Id}"
            : $"ran {executed} functions");
        return executed;
    }

    //ExecuteWith runs a function with the compound tag read from the whole with target
    private static int ExecuteWith(CommandContext<CommandSourceStack> context, IDataAccessor accessor)
        => Execute(context, accessor.GetData());

    //ExecuteWithPath runs a function with the compound tag on the given path, maps to vanilla getArgumentTag
    private static int ExecuteWithPath(CommandContext<CommandSourceStack> context, IDataAccessor accessor)
    {
        var tags = NbtPathArgument.GetPath(context, "path").Get(accessor.GetData());
        if (tags.Count != 1 || tags[0] is not CompoundTag compound)
            throw ErrorArgumentNotCompound.Create(tags.Count == 1 ? "non-compound tag" : $"{tags.Count} tags");
        return Execute(context, compound);
    }

    private static RequiredArgumentBuilder<CommandSourceStack, T> Arg<T>(string name, ArgumentType<T> type)
        => RequiredArgumentBuilder<CommandSourceStack, T>.Argument(name, type);
}
