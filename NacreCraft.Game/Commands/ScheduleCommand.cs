using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Functions;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.World.Level.Timers;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands;

//ScheduleCommand schedule command, maps to vanilla net.minecraft.server.commands.ScheduleCommand
//schedule function <function> <time> [append|replace] queues a function or tag through the server TimerQueue
//schedule clear <id> removes every event with that id; time units follow TimeArgument (d/s/t)
public static class ScheduleCommand
{
    private static readonly SimpleCommandExceptionType ErrorSameTick =
        new(new LiteralMessage("cannot schedule a function in the same tick"));

    private static readonly DynamicCommandExceptionType ErrorCantRemove =
        new(id => new LiteralMessage($"cannot remove scheduled event {id}"));

    private static readonly SimpleCommandExceptionType ErrorMacro =
        new(new LiteralMessage("cannot schedule a macro function"));

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("schedule")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("function")
                .Then(RequiredArgumentBuilder<CommandSourceStack, FunctionArgument.Result>.Argument(
                        "function", FunctionArgument.Functions())
                    .Suggests(SuggestFunctions)
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                            "time", TimeArgument.Time())
                        .Executes(context => Schedule(context, true))
                        .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("append")
                            .Executes(context => Schedule(context, false)))
                        .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("replace")
                            .Executes(context => Schedule(context, true))))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("clear")
                .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument(
                        "function", StringArgumentType.GreedyString())
                    .Suggests(SuggestScheduledIds)
                    .Executes(Remove))));
    }

    //Schedule queues the function or tag at the level game time plus the delay, maps to vanilla schedule
    private static int Schedule(CommandContext<CommandSourceStack> context, bool replace)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        var time = IntegerArgumentType.GetInteger(context, "time");
        if (time == 0) throw ErrorSameTick.Create();

        var result = context.GetArgument<FunctionArgument.Result>("function");
        //Resolve now so an unknown function or tag is reported while the command runs, like vanilla FunctionArgument
        var (callbackId, functions) = result.UnwrapToCollection(context);
        var level = source.Level ?? source.Server.Overworld;
        var tickTime = level.GameTime + time;
        var queue = source.Server.ScheduledEvents;

        if (!result.IsTag)
        {
            if (functions[0] is MacroFunction<CommandSourceStack>) throw ErrorMacro.Create();
            var id = callbackId.ToString();
            if (replace) queue.Remove(id);
            queue.Schedule(id, tickTime, new FunctionCallback(callbackId));
            source.SendSuccess($"scheduled function {callbackId} in {time} ticks at {tickTime}");
        }
        else
        {
            var id = "#" + callbackId;
            if (replace) queue.Remove(id);
            queue.Schedule(id, tickTime, new FunctionTagCallback(callbackId));
            source.SendSuccess($"scheduled function tag {callbackId} in {time} ticks at {tickTime}");
        }
        return (int)(((tickTime % int.MaxValue) + int.MaxValue) % int.MaxValue);
    }

    //Remove clears every event with the given id, maps to vanilla remove
    private static int Remove(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        var id = StringArgumentType.GetString(context, "function");
        var count = source.Server.ScheduledEvents.Remove(id);
        if (count == 0) throw ErrorCantRemove.Create(id);
        source.SendSuccess($"removed {count} scheduled event(s) with id {id}");
        return count;
    }

    //SuggestFunctions suggests loaded functions and function tags, maps to vanilla SUGGEST_FUNCTION
    private static Task<Suggestions> SuggestFunctions(CommandContext<CommandSourceStack> context,
        SuggestionsBuilder builder)
    {
        if (context.GetSource() is ServerCommandSource source)
        {
            foreach (var id in source.Server.Functions.Library.Functions.Keys)
                AddIfMatches(builder, id.ToString());
            foreach (var tag in source.Server.Functions.Library.AvailableTags)
                AddIfMatches(builder, "#" + tag);
        }
        return builder.BuildFuture();
    }

    //SuggestScheduledIds suggests the ids currently present in the scheduled event queue
    private static Task<Suggestions> SuggestScheduledIds(CommandContext<CommandSourceStack> context,
        SuggestionsBuilder builder)
    {
        if (context.GetSource() is ServerCommandSource source)
            foreach (var id in source.Server.ScheduledEvents.GetEventsIds())
                AddIfMatches(builder, id);
        return builder.BuildFuture();
    }

    private static void AddIfMatches(SuggestionsBuilder builder, string value)
    {
        if (value.StartsWith(builder.Remaining, StringComparison.Ordinal)) builder.Add(value);
    }
}
