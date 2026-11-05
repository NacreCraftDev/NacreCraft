using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using NacreCraft.Util;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.Commands;

//RandomCommand random command, maps to vanilla net.minecraft.server.commands.RandomCommand
//random value|roll <range> [<sequence>] draws an integer and returns it, roll also announces it to everyone
//The sequence store is not ported, so a sequence argument seeds from the world seed and its identifier instead of a tracked sequence
//random reset is registered with the vanilla tree but reports that sequence state is unavailable
public static class RandomCommand
{
    private static readonly SimpleCommandExceptionType ErrorRangeTooLarge =
        new(new LiteralMessage("the range is too large"));

    private static readonly SimpleCommandExceptionType ErrorRangeTooSmall =
        new(new LiteralMessage("the range is too small"));

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("random")
            .Then(DrawRandomValueTree("value", false))
            .Then(DrawRandomValueTree("roll", true))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("reset")
                .Requires(s => s.HasPermission(2))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("*")
                    .Executes(ResetAll)
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument("seed", IntegerArgumentType.Integer())
                        .Executes(context => ResetAllWithSeed(context, true, true))
                        .Then(RequiredArgumentBuilder<CommandSourceStack, bool>.Argument("includeWorldSeed", BoolArgumentType.Bool())
                            .Executes(context => ResetAllWithSeed(context,
                                BoolArgumentType.GetBool(context, "includeWorldSeed"), true))
                            .Then(RequiredArgumentBuilder<CommandSourceStack, bool>.Argument("includeSequenceId", BoolArgumentType.Bool())
                                .Executes(context => ResetAllWithSeed(context,
                                    BoolArgumentType.GetBool(context, "includeWorldSeed"),
                                    BoolArgumentType.GetBool(context, "includeSequenceId")))))))
                .Then(RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument("sequence", IdentifierArgument.Id())
                    .Executes(ResetSequence)
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument("seed", IntegerArgumentType.Integer())
                        .Executes(context => ResetSequenceWithSeed(context, true, true))
                        .Then(RequiredArgumentBuilder<CommandSourceStack, bool>.Argument("includeWorldSeed", BoolArgumentType.Bool())
                            .Executes(context => ResetSequenceWithSeed(context,
                                BoolArgumentType.GetBool(context, "includeWorldSeed"), true))
                            .Then(RequiredArgumentBuilder<CommandSourceStack, bool>.Argument("includeSequenceId", BoolArgumentType.Bool())
                                .Executes(context => ResetSequenceWithSeed(context,
                                    BoolArgumentType.GetBool(context, "includeWorldSeed"),
                                    BoolArgumentType.GetBool(context, "includeSequenceId")))))))));
    }

    //DrawRandomValueTree builds the value/roll subtree, maps to vanilla drawRandomValueTree
    private static LiteralArgumentBuilder<CommandSourceStack> DrawRandomValueTree(string name, bool announce)
        => LiteralArgumentBuilder<CommandSourceStack>.Literal(name)
            .Then(RequiredArgumentBuilder<CommandSourceStack, MinMaxBounds.Ints>.Argument(
                    "range", RangeArgument<MinMaxBounds.Ints>.IntRange())
                .Executes(context => RandomSample(context,
                    context.GetArgument<MinMaxBounds.Ints>("range"), null, announce))
                .Then(RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument("sequence", IdentifierArgument.Id())
                    .Requires(s => s.HasPermission(2))
                    .Executes(context => RandomSample(context,
                        context.GetArgument<MinMaxBounds.Ints>("range"),
                        IdentifierArgument.GetId(context, "sequence"), announce))));

    //RandomSample draws a value from the range and replies, maps to vanilla randomSample
    private static int RandomSample(CommandContext<CommandSourceStack> context, MinMaxBounds.Ints range,
        Identifier? sequence, bool announce)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        var min = range.Min ?? int.MinValue;
        var max = range.Max ?? int.MaxValue;
        var span = (long)max - min;
        if (span == 0) throw ErrorRangeTooSmall.Create();
        if (span >= int.MaxValue) throw ErrorRangeTooLarge.Create();

        //Without a sequence store the per-sequence state cannot be kept; seeding from the identity keeps repeated draws distinct
        var seed = source.Server.WorldSeed;
        if (sequence is { } sequenceId) seed ^= sequenceId.ToString().GetHashCode();
        var value = Mth.RandomBetweenInclusive(RandomSource.Create(seed), min, max);
        if (announce)
            source.Server.PlayerList.BroadcastSystemMessage(
                Component.Literal($"{source.SenderName} rolled {value} (between {min} and {max})"), false);
        else
            source.SendSuccess(value.ToString());
        return value;
    }

    //ResetAll clears every sequence, maps to vanilla resetAllSequences
    private static int ResetAll(CommandContext<CommandSourceStack> context) => ResetUnavailable(context);

    //ResetAllWithSeed clears every sequence and updates the defaults, maps to vanilla resetAllSequencesAndSetNewDefaults
    private static int ResetAllWithSeed(CommandContext<CommandSourceStack> context, bool includeWorldSeed,
        bool includeSequenceId) => ResetUnavailable(context);

    //ResetSequence resets one sequence, maps to vanilla resetSequence
    private static int ResetSequence(CommandContext<CommandSourceStack> context) => ResetUnavailable(context);

    //ResetSequenceWithSeed resets one sequence with explicit salt options, maps to the vanilla overload
    private static int ResetSequenceWithSeed(CommandContext<CommandSourceStack> context, bool includeWorldSeed,
        bool includeSequenceId) => ResetUnavailable(context);

    //ResetUnavailable reset needs a persistent sequence store, which has no counterpart in this project
    private static int ResetUnavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("random sequences are not implemented on this server; nothing was reset");
        return 0;
    }
}
