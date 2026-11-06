using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Tree;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Commands.Data;
using NacreCraft.Game.World.Scores;
using NacreCraft.Nbt;

namespace NacreCraft.Game.Commands;

//ExecuteCommand store tree, maps to the store half of vanilla ExecuteCommand
//result stores the command result, success stores 1/0; each branch installs a chained callback on the command source
//Degraded: store bossbar needs the CustomBossEvents server store, which the server layer does not expose yet
public static partial class ExecuteCommand
{
    //BuildStore attaches store result/success, maps to vanilla wrapStores
    private static LiteralArgumentBuilder<CommandSourceStack> BuildStore(CommandNode<CommandSourceStack> execute,
        LiteralArgumentBuilder<CommandSourceStack> parent)
    {
        parent.Then(BuildStoreBranch(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("result"), true));
        parent.Then(BuildStoreBranch(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("success"), false));
        return parent;
    }

    //BuildStoreBranch one result/success branch
    private static LiteralArgumentBuilder<CommandSourceStack> BuildStoreBranch(CommandNode<CommandSourceStack> execute,
        LiteralArgumentBuilder<CommandSourceStack> literal, bool storeResult)
    {
        literal.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("score")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Then(Arg<string>("objective", ObjectiveArgument.Objective())
                    .Redirect(execute, context => StoreScore(context, storeResult)))));

        literal.Then(BuildStoreData(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("block"),
            BlockAccessor, storeResult));
        literal.Then(BuildStoreData(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("entity"),
            EntityAccessor, storeResult));
        literal.Then(BuildStoreData(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("storage"),
            StorageAccessor, storeResult));
        return literal;
    }

    //BuildStoreData the block/entity/storage data branches with the six numeric widths
    private static LiteralArgumentBuilder<CommandSourceStack> BuildStoreData(
        CommandNode<CommandSourceStack> execute, LiteralArgumentBuilder<CommandSourceStack> literal,
        Func<CommandContext<CommandSourceStack>, IDataAccessor> accessor, bool storeResult)
    {
        var path = Arg<NbtPath>("path", NbtPathArgument.NbtPathArg());
        path.Then(StoreScale(execute, "int", accessor, storeResult, value => new IntTag(value)));
        path.Then(StoreScale(execute, "float", accessor, storeResult, value => new FloatTag(value)));
        path.Then(StoreScale(execute, "short", accessor, storeResult, value => new ShortTag((short)value)));
        path.Then(StoreScale(execute, "long", accessor, storeResult, value => new LongTag(value)));
        path.Then(StoreScale(execute, "double", accessor, storeResult, value => new DoubleTag(value)));
        path.Then(StoreScale(execute, "byte", accessor, storeResult, value => new ByteTag((byte)value)));
        literal.Then(path);
        return literal;
    }

    //StoreScale one numeric width under a data path, maps to the int/float/short/long/double/byte branches
    private static ArgumentBuilder<CommandSourceStack> StoreScale(CommandNode<CommandSourceStack> execute,
        string name, Func<CommandContext<CommandSourceStack>, IDataAccessor> accessor, bool storeResult,
        Func<int, Tag> constructor)
    {
        return Arg<double>("scale", DoubleArgumentType.DoubleArg())
            .Redirect(execute, context => StoreData(context, accessor(context),
                NbtPathArgument.GetPath(context, "path"),
                value => constructor((int)(value * DoubleArgumentType.GetDouble(context, "scale"))),
                storeResult));
    }

    //StoreScore writes the result into every named score, maps to vanilla storeValue(ScoreHolder...)
    private static CommandSourceStack StoreScore(CommandContext<CommandSourceStack> context, bool storeResult)
    {
        var source = SourceOf(context);
        var scoreboard = source.Server.Scoreboard;
        var objective = ResolveObjective(scoreboard, context.GetArgument<string>("objective"));
        var names = context.GetArgument<ScoreHolderArgument.Result>("targets")(context.GetSource());
        var holders = names.Count == 1 && names.First() == "*"
            ? new[] { ScoreHolder.Wildcard }
            : names.Select(ScoreHolder.ForNameOnly).ToArray();
        var callback = new DelegateResultCallback((success, result) =>
        {
            var value = storeResult ? result : success ? 1 : 0;
            foreach (var holder in holders)
                scoreboard.GetOrCreatePlayerScore(holder, objective).Set(value);
        });
        return source.WithCallback(CommandResultCallback.Chain(source.Callback, callback));
    }

    //StoreData writes the result into an NBT path, maps to vanilla storeData
    private static CommandSourceStack StoreData(CommandContext<CommandSourceStack> context, IDataAccessor accessor,
        NbtPath path, Func<int, Tag> constructor, bool storeResult)
    {
        var source = SourceOf(context);
        var callback = new DelegateResultCallback((success, result) =>
        {
            try
            {
                var data = accessor.GetData();
                var value = storeResult ? result : success ? 1 : 0;
                path.Set(data, constructor(value));
                accessor.SetData(data);
            }
            catch (CommandSyntaxException)
            {
                //The vanilla store silently ignores a path that cannot be written
            }
        });
        return source.WithCallback(CommandResultCallback.Chain(source.Callback, callback));
    }
}
