using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Execution;
using NacreCraft.Commands.Execution.Tasks;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Functions;
using NacreCraft.Commands.Tree;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Commands.Data;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Scores;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Commands;

//ExecuteCommand condition tree, maps to the if/unless half of vanilla ExecuteCommand
//Every condition has two shapes: a fork that filters the source list, and a bare execute that returns a count or throws
//Degraded: if items needs the SlotProvider container wiring, if predicate needs the loot predicate system, and the on relations
//need the entity relationship interfaces; those branches are documented in the OFAP entry
public static partial class ExecuteCommand
{
    //MaxTestArea block limit for if blocks, maps to vanilla MAX_TEST_AREA
    private const int MaxTestArea = 32768;

    //ErrorConditionalFailed the bare condition failed, maps to vanilla commands.execute.conditional.fail
    private static readonly SimpleCommandExceptionType ErrorConditionalFailed =
        new(new LiteralMessage("Test failed"));

    //ErrorConditionalFailedCount the bare unless failed and matched this many, maps to vanilla conditional.fail_count
    private static readonly DynamicCommandExceptionType ErrorConditionalFailedCount =
        new(count => new LiteralMessage($"Test failed, count: {count}"));

    //ErrorAreaTooLarge the if blocks region is over the limit, maps to vanilla commands.execute.conditional.too_many_blocks
    private static readonly Dynamic2CommandExceptionType ErrorAreaTooLarge =
        new((max, count) => new LiteralMessage($"Too many blocks in the specified area (maximum {max}, specified {count})"));

    //ErrorObjectiveNotFound objective name did not resolve, maps to vanilla argument.objective.notfound
    private static readonly DynamicCommandExceptionType ErrorObjectiveNotFound =
        new(name => new LiteralMessage($"Unknown scoreboard objective '{name}'"));

    //ErrorScoreHolderEmpty a score condition needs exactly one holder, maps to vanilla argument.scoreHolder.empty
    private static readonly SimpleCommandExceptionType ErrorScoreHolderEmpty =
        new(new LiteralMessage("Expected a single score holder"));

    //ErrorStopwatchMissing the stopwatch id does not exist, maps to vanilla commands.stopwatch.does_not_exist
    private static readonly DynamicCommandExceptionType ErrorStopwatchMissing =
        new(id => new LiteralMessage($"Stopwatch '{id}' does not exist"));

    //CommandPredicate a boolean condition over the parsed context
    private delegate bool CommandPredicate(CommandContext<CommandSourceStack> context);

    //CommandNumericPredicate a counting condition over the parsed context
    private delegate int CommandNumericPredicate(CommandContext<CommandSourceStack> context);

    //IntBiPredicate the score comparison operator
    private delegate bool IntBiPredicate(int left, int right);

    //BuildConditionals attaches the whole if/unless subtree, maps to vanilla addConditionals
    private static LiteralArgumentBuilder<CommandSourceStack> BuildConditionals(CommandNode<CommandSourceStack> execute,
        LiteralArgumentBuilder<CommandSourceStack> parent, bool expected)
    {
        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("block")
            .Then(Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos())
                .Then(AddConditional(execute, Arg<Predicate<BlockInWorld>>("block", BlockPredicateArgument.BlockPredicate()),
                    expected, context => BlockPredicateArgument.GetBlockPredicate(context, "block")(
                        new BlockInWorld(RequirePersistent(context),
                            BlockPosArgument.GetLoadedBlockPos(context, "pos"),
                            SourceOf(context).Server.BlockEntities))))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("biome")
            .Then(Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos())
                .Then(AddConditional(execute,
                    Arg<ResourceOrTagArgument<Biome>.Result>("biome",
                        new ResourceOrTagArgument<Biome>(BuiltInRegistries.BIOME)), expected,
                    context => BiomeMatches(context,
                        ((ServerCommandSource)context.GetSource()).Level?.GetBiome(
                            BlockPosArgument.GetLoadedBlockPos(context, "pos")))))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("loaded")
            .Then(AddConditional(execute, Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos()), expected,
                context => IsChunkLoaded(SourceOf(context).Level, BlockPosArgument.GetBlockPos(context, "pos")))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("dimension")
            .Then(AddConditional(execute, Arg<Identifier>("dimension", DimensionArgument.Dimension()), expected,
                context => ReferenceEquals(DimensionArgument.GetDimension(context, "dimension"),
                    SourceOf(context).Level))));

        parent.Then(BuildScoreConditionals(execute, expected));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("blocks")
            .Then(Arg<BlockCoordinates>("start", BlockPosArgument.BlockPos())
                .Then(Arg<BlockCoordinates>("end", BlockPosArgument.BlockPos())
                    .Then(Arg<BlockCoordinates>("destination", BlockPosArgument.BlockPos())
                        .Then(AddBlocksConditional(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("all"),
                            expected, false))
                        .Then(AddBlocksConditional(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("masked"),
                            expected, true))))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("entity")
            .Then(Arg<EntitySelector>("entities", EntityArgument.Entities())
                .Fork(execute, context => Expect(context, expected,
                    EntityArgument.GetOptionalEntities(context, "entities").Count > 0))
                .Executes(CreateNumericConditionalHandler(expected,
                    context => EntityArgument.GetOptionalEntities(context, "entities").Count))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("data")
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("block")
                .Then(Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos())
                    .Then(Arg<NbtPath>("path", NbtPathArgument.NbtPathArg())
                        .Fork(execute, context => Expect(context, expected, CheckMatchingData(context, BlockAccessor(context)) > 0))
                        .Executes(CreateNumericConditionalHandler(expected,
                            context => CheckMatchingData(context, BlockAccessor(context)))))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("entity")
                .Then(Arg<EntitySelector>("target", EntityArgument.Entity())
                    .Then(Arg<NbtPath>("path", NbtPathArgument.NbtPathArg())
                        .Fork(execute, context => Expect(context, expected, CheckMatchingData(context, EntityAccessor(context)) > 0))
                        .Executes(CreateNumericConditionalHandler(expected,
                            context => CheckMatchingData(context, EntityAccessor(context)))))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("storage")
                .Then(Arg<Identifier>("source", IdentifierArgument.Id())
                    .Then(Arg<NbtPath>("path", NbtPathArgument.NbtPathArg())
                        .Fork(execute, context => Expect(context, expected, CheckMatchingData(context, StorageAccessor(context)) > 0))
                        .Executes(CreateNumericConditionalHandler(expected,
                            context => CheckMatchingData(context, StorageAccessor(context))))))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("function")
            .Then(Arg<FunctionArgument.Result>("name", FunctionArgument.Functions())
                .ForwardCustom(execute, new ExecuteIfFunctionModifier(expected), true)));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("stopwatch")
            .Then(Arg<Identifier>("id", IdentifierArgument.Id())
                .Then(AddConditional(execute, Arg<MinMaxBounds.Doubles>("range", new RangeArgument<MinMaxBounds.Doubles>.Floats()),
                    expected, context => CheckStopwatch(context)))));

        return parent;
    }

    //BuildScoreConditionals the if score subtree, maps to the score branch of vanilla addConditionals
    private static LiteralArgumentBuilder<CommandSourceStack> BuildScoreConditionals(
        CommandNode<CommandSourceStack> execute, bool expected)
    {
        var targetObjective = Arg<string>("targetObjective", ObjectiveArgument.Objective())
            .Then(BuildScoreOperator(execute, "=", (left, right) => left == right, expected))
            .Then(BuildScoreOperator(execute, "<", (left, right) => left < right, expected))
            .Then(BuildScoreOperator(execute, "<=", (left, right) => left <= right, expected))
            .Then(BuildScoreOperator(execute, ">", (left, right) => left > right, expected))
            .Then(BuildScoreOperator(execute, ">=", (left, right) => left >= right, expected))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("matches")
                .Then(AddConditional(execute,
                    Arg<MinMaxBounds.Ints>("range", new RangeArgument<MinMaxBounds.Ints>.Ints()), expected,
                    context => ScoreMatches(context))));
        return LiteralArgumentBuilder<CommandSourceStack>.Literal("score")
            .Then(Arg<ScoreHolderArgument.Result>("target", ScoreHolderArgument.ScoreHolder())
                .Then(targetObjective));
    }

    //BuildScoreOperator one comparison operator under if score
    private static LiteralArgumentBuilder<CommandSourceStack> BuildScoreOperator(
        CommandNode<CommandSourceStack> execute, string op, IntBiPredicate test, bool expected)
        => LiteralArgumentBuilder<CommandSourceStack>.Literal(op)
            .Then(Arg<ScoreHolderArgument.Result>("source", ScoreHolderArgument.ScoreHolder())
                .Then(AddConditional(execute, Arg<string>("sourceObjective", ObjectiveArgument.Objective()), expected,
                    context => CompareScores(context, test))));

    //RegisterOn attaches the on relations; the relation interfaces are not ported, so every relation yields no source
    private static LiteralArgumentBuilder<CommandSourceStack> BuildRelations(CommandNode<CommandSourceStack> execute,
        LiteralArgumentBuilder<CommandSourceStack> parent)
    {
        foreach (var relation in new[] { "owner", "leasher", "target", "attacker", "vehicle", "controller", "origin", "passengers" })
        {
            parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal(relation)
                .Fork(execute, _ => Array.Empty<CommandSourceStack>()));
        }
        return parent;
    }

    //AddConditional forks on the condition and executes it bare, maps to vanilla addConditional
    private static TB AddConditional<TB>(CommandNode<CommandSourceStack> root, TB argument, bool expected,
        CommandPredicate predicate) where TB : ArgumentBuilder<CommandSourceStack, TB>
    {
        argument.Fork(root, context => Expect(context, expected, predicate(context)));
        argument.Executes(context =>
        {
            if (expected == predicate(context))
            {
                context.GetSource().SendSuccess("Test passed");
                return 1;
            }
            throw ErrorConditionalFailed.Create();
        });
        return argument;
    }

    //AddBlocksConditional the if blocks all/masked branch, maps to vanilla addIfBlocksConditional
    private static TB AddBlocksConditional<TB>(CommandNode<CommandSourceStack> root, TB argument, bool expected,
        bool skipAir) where TB : ArgumentBuilder<CommandSourceStack, TB>
    {
        argument.Fork(root, context => Expect(context, expected, CheckRegions(context, skipAir) is not null));
        argument.Executes(expected ? CheckIfRegions : CheckUnlessRegions);
        return argument;
    }

    //Expect the source list after a condition, empty when the result differs from the expectation
    private static ICollection<CommandSourceStack> Expect(CommandContext<CommandSourceStack> context, bool expected,
        bool result)
        => result == expected ? new[] { context.GetSource() } : Array.Empty<CommandSourceStack>();

    //CreateNumericConditionalHandler a counting condition reported as its own result, maps to vanilla createNumericConditionalHandler
    private static Command<CommandSourceStack> CreateNumericConditionalHandler(bool expected,
        CommandNumericPredicate condition)
    {
        if (expected)
        {
            return context =>
            {
                var count = condition(context);
                if (count > 0)
                {
                    context.GetSource().SendSuccess($"Test passed, count: {count}");
                    return count;
                }
                throw ErrorConditionalFailed.Create();
            };
        }
        return context =>
        {
            var count = condition(context);
            if (count == 0)
            {
                context.GetSource().SendSuccess("Test passed");
                return 1;
            }
            throw ErrorConditionalFailedCount.Create(count);
        };
    }

    //CheckIfRegions the bare if blocks form, maps to vanilla checkIfRegions
    private static int CheckIfRegions(CommandContext<CommandSourceStack> context)
    {
        var count = CheckRegions(context, false) ?? throw ErrorConditionalFailed.Create();
        context.GetSource().SendSuccess($"Test passed, count: {count}");
        return count;
    }

    //CheckUnlessRegions the bare unless blocks form, maps to vanilla checkUnlessRegions
    private static int CheckUnlessRegions(CommandContext<CommandSourceStack> context)
    {
        var count = CheckRegions(context, false);
        if (count is { } matched) throw ErrorConditionalFailedCount.Create(matched);
        context.GetSource().SendSuccess("Test passed");
        return 1;
    }

    //CheckRegions compares two regions block by block, null when they differ, maps to vanilla checkRegions
    //Block entity data is not compared yet: the block entity NBT export used by vanilla is not wired to the level here
    private static int? CheckRegions(CommandContext<CommandSourceStack> context, bool skipAir)
    {
        var level = SourceOf(context).Level ?? throw BlockPosArgument.ErrorNotLoaded.Create();
        var start = BlockPosArgument.GetLoadedBlockPos(context, "start");
        var end = BlockPosArgument.GetLoadedBlockPos(context, "end");
        var destination = BlockPosArgument.GetLoadedBlockPos(context, "destination");
        var minX = Math.Min(start.X, end.X);
        var minY = Math.Min(start.Y, end.Y);
        var minZ = Math.Min(start.Z, end.Z);
        var maxX = Math.Max(start.X, end.X);
        var maxY = Math.Max(start.Y, end.Y);
        var maxZ = Math.Max(start.Z, end.Z);
        var area = (long)(maxX - minX + 1) * (maxY - minY + 1) * (maxZ - minZ + 1);
        if (area > MaxTestArea) throw ErrorAreaTooLarge.Create(MaxTestArea, area);
        var offset = new BlockPos(destination.X - minX, destination.Y - minY, destination.Z - minZ);
        var count = 0;
        for (var z = minZ; z <= maxZ; z++)
        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var sourcePos = new BlockPos(x, y, z);
            var sourceState = level.GetBlockState(sourcePos);
            if (sourceState is not { } state) continue;
            if (skipAir && state.IsAir) continue;
            var destinationPos = sourcePos.Offset(offset.X, offset.Y, offset.Z);
            if (level.GetBlockState(destinationPos) is not { } destinationState || destinationState != state)
                return null;
            count++;
        }
        return count;
    }

    //BiomeMatches tests a parsed resource-or-tag result against the holder at the position, maps to ResourceOrTagArgument.Result.test
    private static bool BiomeMatches(CommandContext<CommandSourceStack> context, Holder<Biome>? holder)
    {
        if (holder is null) return false;
        var result = context.GetArgument<ResourceOrTagArgument<Biome>.Result>("biome");
        if (result.Element is { } element) return ReferenceEquals(element.Value, holder.Value);
        return result.Tag?.Contains(holder) ?? false;
    }

    //IsChunkLoaded whether the chunk is loaded, maps to vanilla isChunkLoaded
    private static bool IsChunkLoaded(ServerLevel? level, BlockPos pos)
        => level is not null && level.GetChunk(new ChunkPos(pos.X >> 4, pos.Z >> 4)) is not null;

    //CompareScores reads both scores and applies the operator, maps to vanilla checkScore
    private static bool CompareScores(CommandContext<CommandSourceStack> context, IntBiPredicate operation)
    {
        var scoreboard = SourceOf(context).Server.Scoreboard;
        var targetObjective = ResolveObjective(scoreboard, context.GetArgument<string>("targetObjective"));
        var sourceObjective = ResolveObjective(scoreboard, context.GetArgument<string>("sourceObjective"));
        var target = GetScoreHolder(context, "target");
        var source = GetScoreHolder(context, "source");
        var left = scoreboard.GetPlayerScoreInfo(target, targetObjective);
        var right = scoreboard.GetPlayerScoreInfo(source, sourceObjective);
        if (left is null || right is null) return false;
        return operation(left.Value(), right.Value());
    }

    //ScoreMatches reads the target score and matches it against the range, maps to the matches branch of checkScore
    private static bool ScoreMatches(CommandContext<CommandSourceStack> context)
    {
        var scoreboard = SourceOf(context).Server.Scoreboard;
        var objective = ResolveObjective(scoreboard, context.GetArgument<string>("targetObjective"));
        var score = scoreboard.GetPlayerScoreInfo(GetScoreHolder(context, "target"), objective);
        return score is not null && context.GetArgument<MinMaxBounds.Ints>("range").Matches(score.Value());
    }

    //CheckStopwatch reads the elapsed seconds and matches the range, maps to vanilla checkStopwatch
    private static bool CheckStopwatch(CommandContext<CommandSourceStack> context)
    {
        var source = SourceOf(context);
        var id = IdentifierArgument.GetId(context, "id");
        var start = source.Server.Stopwatches.GetStart(id) ?? throw ErrorStopwatchMissing.Create(id);
        var elapsedSeconds = (NacreCraft.Game.Server.Stopwatches.CurrentTime() - start) / 1000.0;
        return context.GetArgument<MinMaxBounds.Doubles>("range").Matches(elapsedSeconds);
    }

    //CheckMatchingData counts the matches on the path, maps to vanilla checkMatchingData
    private static int CheckMatchingData(CommandContext<CommandSourceStack> context, IDataAccessor accessor)
        => NbtPathArgument.GetPath(context, "path").CountMatching(accessor.GetData());

    //ResolveObjective looks up an objective by name, maps to vanilla ObjectiveArgument.getObjective
    private static Objective ResolveObjective(Scoreboard scoreboard, string name)
        => scoreboard.GetObjective(name) ?? throw ErrorObjectiveNotFound.Create(name);

    //GetScoreHolder resolves a single score holder, maps to vanilla ScoreHolderArgument.getName
    private static ScoreHolder GetScoreHolder(CommandContext<CommandSourceStack> context, string name)
    {
        var names = context.GetArgument<ScoreHolderArgument.Result>(name)(context.GetSource());
        return names.Count == 1 ? ScoreHolder.ForNameOnly(names.First()) : throw ErrorScoreHolderEmpty.Create();
    }

    //BlockAccessor builds the block entity data accessor, shared by if data and store
    private static IDataAccessor BlockAccessor(CommandContext<CommandSourceStack> context)
    {
        var source = SourceOf(context);
        var pos = BlockPosArgument.GetLoadedBlockPos(context, "pos");
        var level = RequirePersistent(context);
        if (level.BlockUpdateSink?.GetBlockEntity(pos) is not BlockEntity entity)
            throw BlockDataAccessor.ErrorNotBlockEntity.Create();
        return new BlockDataAccessor(level, source.Server.PlayerList, entity, pos);
    }

    //EntityAccessor builds the entity data accessor, shared by if data and store
    private static IDataAccessor EntityAccessor(CommandContext<CommandSourceStack> context)
        => new EntityDataAccessor(EntityArgument.GetSingleTarget(context, "target"));

    //StorageAccessor builds the command storage accessor, shared by if data and store
    private static IDataAccessor StorageAccessor(CommandContext<CommandSourceStack> context)
        => new StorageDataAccessor(SourceOf(context).Server.CommandStorage,
            IdentifierArgument.GetId(context, "source"));

    //RequirePersistent the source level as a persistent level, maps to the vanilla block entity access requirement
    private static PersistentServerLevel RequirePersistent(CommandContext<CommandSourceStack> context)
        => SourceOf(context).Level as PersistentServerLevel ?? throw BlockPosArgument.ErrorNotLoaded.Create();

    //ExecuteIfFunctionModifier runs the named functions in an isolated frame and keeps the sources whose result passes
    //maps to vanilla ExecuteIfFunctionCustomModifier + scheduleFunctionConditionsAndTest
    private sealed class ExecuteIfFunctionModifier(bool expected) : CustomModifierExecutor<CommandSourceStack>
    {
        public void Apply(CommandSourceStack originalSource, List<CommandSourceStack> sources,
            ContextChain<CommandSourceStack> chain, ChainModifiers modifiers,
            ExecutionControl<CommandSourceStack> output)
        {
            var original = originalSource as ServerCommandSource
                           ?? throw new InvalidOperationException("/execute requires a server command source");
            try
            {
                var functions = FunctionArgument.GetFunctions(chain.GetTopContext().CopyFor(originalSource), "name");
                if (functions.Count == 0) return;
                var dispatcher = original.Server.Commands.Dispatcher;
                var instantiated = new List<InstantiatedFunction<CommandSourceStack>>(functions.Count);
                foreach (var function in functions)
                    instantiated.Add(function.Instantiate(null, dispatcher));
                var filteredSources = new List<CommandSourceStack>();
                foreach (var source in sources)
                {
                    //The isolated frame reports through this callback; the isolated source itself carries no output
                    var executionSource = (ServerCommandSource)source.WithCallback(CommandResultCallback.Empty);
                    var functionCallback = new DelegateResultCallback((_, result) =>
                    {
                        if (expected ? result != 0 : result == 0) filteredSources.Add(source);
                    });
                    output.QueueNext(new IsolatedCall<CommandSourceStack>(control =>
                    {
                        foreach (var function in instantiated)
                        {
                            control.QueueNext(new CallFunction<CommandSourceStack>(function,
                                    control.CurrentFrame().ReturnValueConsumer, true)
                                .ToUnboundAction().Bind(executionSource));
                        }
                        control.QueueNext(FallthroughTask<CommandSourceStack>.Instance());
                    }, functionCallback).ToEntryAction());
                }
                var nextStage = chain.NextStage();
                if (nextStage is null) return;
                var input = chain.GetTopContext().GetInput();
                output.QueueNext(new BuildContexts<CommandSourceStack>.Continuation(input, nextStage, modifiers,
                    originalSource, filteredSources).Execute);
            }
            catch (CommandSyntaxException e)
            {
                originalSource.HandleError(e.Type, e.RawMessage, modifiers.IsForked(), output.Tracer());
            }
        }
    }

    //DelegateResultCallback a callback built from a delegate, used by conditions and stores
    private sealed class DelegateResultCallback(Action<bool, int> action) : CommandResultCallback
    {
        public void OnResult(bool success, int result) => action(success, result);
    }
}
