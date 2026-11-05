using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Bootstrap;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Storage.Updates;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.Commands;

//PlaceCommand place command, maps to vanilla net.minecraft.server.commands.PlaceCommand
//template and jigsaw are really implemented on the structure template manager and JigsawPlacement; feature and structure
//need a ChunkGenerator and a GenerationContext that the level does not expose to commands, so those two branches only fail
public static class PlaceCommand
{
    //UnavailableFeature the configured feature registry is loaded but placing one needs the generator and a WorldGenRegion
    private const string FeatureUnavailable = "place feature needs the level chunk generator, which commands cannot reach yet";

    //UnavailableStructure a structure start needs the chunk generator and random state, which commands cannot reach yet
    private const string StructureUnavailable = "place structure needs the level chunk generator, which commands cannot reach yet";

    //UnavailableTemplates the structure template manager is injected after data load; before that templates cannot be read
    private const string TemplatesUnavailable = "no structure template manager is available yet";

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var place = LiteralArgumentBuilder<CommandSourceStack>.Literal("place")
            .Requires(s => s.HasPermission(2));

        var feature = LiteralArgumentBuilder<CommandSourceStack>.Literal("feature")
            .Then(Arg<Identifier>("feature", new ResourceArgument(Registries.CONFIGURED_FEATURE.Identifier))
                .Executes(context => Fail(context, FeatureUnavailable))
                .Then(Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos())
                    .Executes(context => Fail(context, FeatureUnavailable))));
        place.Then(feature);

        var jigsaw = LiteralArgumentBuilder<CommandSourceStack>.Literal("jigsaw")
            .Then(Arg<Identifier>("pool", new ResourceArgument(Registries.TEMPLATE_POOL.Identifier))
                .Then(Arg<Identifier>("target", IdentifierArgument.Id())
                    .Then(Arg<int>("max_depth", IntegerArgumentType.Integer(1, 20))
                        .Executes(context => PlaceJigsaw(context, null))
                        .Then(Arg<BlockCoordinates>("position", BlockPosArgument.BlockPos())
                            .Executes(context => PlaceJigsaw(context, BlockPosArgument.GetBlockPos(context, "position")))))));
        place.Then(jigsaw);

        var structure = LiteralArgumentBuilder<CommandSourceStack>.Literal("structure")
            .Then(Arg<Identifier>("structure", new ResourceArgument(Registries.STRUCTURE.Identifier))
                .Executes(context => Fail(context, StructureUnavailable))
                .Then(Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos())
                    .Executes(context => Fail(context, StructureUnavailable))));
        place.Then(structure);

        RegisterTemplate(place);
        dispatcher.Register(place);
    }

    //RegisterTemplate the template <template> [<pos> [<rotation> [<mirror> [<integrity> [<seed> [strict]]]]]] branch
    private static void RegisterTemplate(LiteralArgumentBuilder<CommandSourceStack> place)
    {
        var strict = LiteralArgumentBuilder<CommandSourceStack>.Literal("strict")
            .Executes(context => PlaceTemplate(context, false,
                context.GetArgument<Rotation>("rotation"), context.GetArgument<Mirror>("mirror"),
                FloatArgumentType.GetFloat(context, "integrity"), IntegerArgumentType.GetInteger(context, "seed"), true));
        var seed = Arg<int>("seed", IntegerArgumentType.Integer())
            .Executes(context => PlaceTemplate(context, false,
                context.GetArgument<Rotation>("rotation"), context.GetArgument<Mirror>("mirror"),
                FloatArgumentType.GetFloat(context, "integrity"), IntegerArgumentType.GetInteger(context, "seed"), false))
            .Then(strict);
        var integrity = Arg<float>("integrity", FloatArgumentType.FloatArg(0f, 1f))
            .Executes(context => PlaceTemplate(context, false,
                context.GetArgument<Rotation>("rotation"), context.GetArgument<Mirror>("mirror"),
                FloatArgumentType.GetFloat(context, "integrity"), 0, false))
            .Then(seed);
        var mirror = Arg<Mirror>("mirror", TemplateMirrorArgument.TemplateMirror())
            .Executes(context => PlaceTemplate(context, false,
                context.GetArgument<Rotation>("rotation"), context.GetArgument<Mirror>("mirror"), 1f, 0, false))
            .Then(integrity);
        var rotation = Arg<Rotation>("rotation", TemplateRotationArgument.TemplateRotation())
            .Executes(context => PlaceTemplate(context, false,
                context.GetArgument<Rotation>("rotation"), Mirror.None, 1f, 0, false))
            .Then(mirror);
        var pos = Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos())
            .Executes(context => PlaceTemplate(context, true, Rotation.None, Mirror.None, 1f, 0, false))
            .Then(rotation);
        var template = Arg<Identifier>("template", IdentifierArgument.Id())
            .Executes(context => PlaceTemplate(context, false, Rotation.None, Mirror.None, 1f, 0, false))
            .Then(pos);

        place.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("template").Then(template));
    }

    //PlaceJigsaw assembles a jigsaw start pool into the level, maps to vanilla placeJigsaw
    private static int PlaceJigsaw(CommandContext<CommandSourceStack> context, BlockPos? position)
    {
        var source = (ServerCommandSource)context.GetSource();
        if (source.Level is not { } level) return Fail(context, "place needs a level context");
        if (GameBootstrap.StructureTemplates is not { } manager) return Fail(context, TemplatesUnavailable);

        var poolId = ResourceArgument.GetResource(context, "pool");
        var startPool = BuiltInRegistries.TEMPLATE_POOL.Get(poolId);
        if (startPool is null) return Fail(context, $"unknown template pool {poolId}");

        var target = IdentifierArgument.GetId(context, "target");
        var depth = IntegerArgumentType.GetInteger(context, "max_depth");
        var origin = source.Position;
        var pos = position ?? new BlockPos((int)Math.Floor(origin.X), (int)Math.Floor(origin.Y), (int)Math.Floor(origin.Z));
        JigsawPlacement.GenerateInWorld(level, manager, startPool, target, depth, pos, false);
        source.SendSuccess($"placed jigsaw {poolId} at {pos.X}, {pos.Y}, {pos.Z}");
        return 1;
    }

    //PlaceTemplate writes a structure template into the level, maps to vanilla placeTemplate
    //The caller fills in the defaults of the optional trail, because the dispatch chain only yields arguments that were typed
    private static int PlaceTemplate(CommandContext<CommandSourceStack> context, bool hasPos, Rotation rotation,
        Mirror mirror, float integrity, int seed, bool strict)
    {
        var source = (ServerCommandSource)context.GetSource();
        if (source.Level is not { } level) return Fail(context, "place needs a level context");
        if (GameBootstrap.StructureTemplates is not { } manager) return Fail(context, TemplatesUnavailable);

        var id = IdentifierArgument.GetId(context, "template");
        var template = manager.GetOrLoad(id);
        if (template is null) return Fail(context, $"unknown structure template {id}");

        var position = source.Position;
        var pos = hasPos
            ? BlockPosArgument.GetBlockPos(context, "pos")
            : new BlockPos((int)Math.Floor(position.X), (int)Math.Floor(position.Y), (int)Math.Floor(position.Z));
        if (!level.IsLoaded(pos)) return Fail(context, "that position is not loaded");

        var random = RandomSource.Create(seed);

        var settings = new StructurePlaceSettings()
            .SetMirror(mirror)
            .SetRotation(rotation)
            .SetKnownShape(strict);
        if (integrity < 1f)
            settings.ClearProcessors().AddProcessor(new BlockRotProcessor(integrity)).SetRandom(random);

        var flags = BlockUpdateFlags.Clients | (strict ? BlockUpdateFlags.SkipAllSideEffects : 0);
        if (!template.PlaceInWorld(level, pos, pos, settings, random, flags))
            return Fail(context, "the structure template has no size");

        source.SendSuccess($"placed template {id} at {pos.X}, {pos.Y}, {pos.Z}");
        return 1;
    }

    private static RequiredArgumentBuilder<CommandSourceStack, T> Arg<T>(string name, ArgumentType<T> type)
        => RequiredArgumentBuilder<CommandSourceStack, T>.Argument(name, type);

    private static int Fail(CommandContext<CommandSourceStack> context, string message)
    {
        context.GetSource().SendFailure(message);
        return 0;
    }
}
