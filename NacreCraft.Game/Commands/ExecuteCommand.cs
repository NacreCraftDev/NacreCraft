using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Tree;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util;

namespace NacreCraft.Game.Commands;

//ExecuteCommand /execute command, maps to vanilla net.minecraft.server.commands.ExecuteCommand
//The chain is a tree of redirects and forks onto the shared execute node: conditions, stores, as/at/positioned/rotated/facing/
//align/anchored/in/summon and the on relations rewrite the source, run dispatches it
public static partial class ExecuteCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        //The bare node is what every redirect and fork lands on, maps to the first vanilla registration
        var execute = dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("execute")
            .Requires(s => s.HasPermission(2)));

        var root = LiteralArgumentBuilder<CommandSourceStack>.Literal("execute")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("run").Redirect(dispatcher.GetRoot()))
            .Then(BuildConditionals(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("if"), true))
            .Then(BuildConditionals(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("unless"), false))
            .Then(BuildStore(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("store")))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("as")
                .Then(Arg<EntitySelector>("targets", EntityArgument.Entities()).Fork(execute, context =>
                {
                    var result = new List<CommandSourceStack>();
                    foreach (var target in EntityArgument.GetOptionalEntities(context, "targets"))
                        result.Add(SourceOf(context).WithEntity(target.Source));
                    return result;
                })))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("at")
                .Then(Arg<EntitySelector>("targets", EntityArgument.Entities()).Fork(execute, context =>
                {
                    var result = new List<CommandSourceStack>();
                    foreach (var target in EntityArgument.GetOptionalEntities(context, "targets"))
                    {
                        var entity = target.Source;
                        var redirected = SourceOf(context).WithPosition(entity.Pos)
                            .WithRotation(entity.YRot, entity.XRot);
                        if (entity.Level is ServerLevel entityLevel) redirected = redirected.WithLevel(entityLevel);
                        result.Add(redirected);
                    }
                    return result;
                })))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("positioned")
                .Then(Arg<Coordinates>("pos", Vec3Argument.Vec3()).Redirect(execute, context
                    => SourceOf(context).WithPosition(Vec3Argument.GetVec3(context, "pos"))
                        .WithAnchor(EntityAnchorArgument.Anchor.Feet)))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("as")
                    .Then(Arg<EntitySelector>("targets", EntityArgument.Entities()).Fork(execute, context =>
                    {
                        var result = new List<CommandSourceStack>();
                        foreach (var target in EntityArgument.GetOptionalEntities(context, "targets"))
                            result.Add(SourceOf(context).WithPosition(target.Source.Pos));
                        return result;
                    })))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("over")
                    .Then(Arg<Heightmap.Types>("heightmap", HeightmapTypeArgument.Heightmap()).Redirect(execute, context =>
                    {
                        var source = SourceOf(context);
                        var position = source.Position;
                        var level = source.Level
                            ?? throw BlockPosArgument.ErrorNotLoaded.Create();
                        var x = Mth.Floor(position.X);
                        var z = Mth.Floor(position.Z);
                        var chunk = level.GetChunk(new ChunkPos(x >> 4, z >> 4))
                            ?? throw BlockPosArgument.ErrorNotLoaded.Create();
                        var height = chunk.GetHeight(HeightmapTypeArgument.GetHeightmap(context, "heightmap"), x, z);
                        return source.WithPosition(new Vec3(position.X, height, position.Z));
                    }))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("rotated")
                .Then(Arg<Coordinates>("rot", RotationArgument.Rotation()).Redirect(execute, context =>
                {
                    var source = SourceOf(context);
                    var (yaw, pitch) = RotationArgument.GetRotation(context, "rot").GetRotation(source);
                    return source.WithRotation(yaw, pitch);
                }))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("as")
                    .Then(Arg<EntitySelector>("targets", EntityArgument.Entities()).Fork(execute, context =>
                    {
                        var result = new List<CommandSourceStack>();
                        foreach (var target in EntityArgument.GetOptionalEntities(context, "targets"))
                            result.Add(SourceOf(context).WithRotation(target.Source.YRot, target.Source.XRot));
                        return result;
                    }))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("facing")
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("entity")
                    .Then(Arg<EntitySelector>("targets", EntityArgument.Entities())
                        .Then(Arg<EntityAnchorArgument.Anchor>("anchor", EntityAnchorArgument.EntityAnchor())
                            .Fork(execute, context =>
                            {
                                var anchor = EntityAnchorArgument.GetAnchor(context, "anchor");
                                var result = new List<CommandSourceStack>();
                                foreach (var target in EntityArgument.GetOptionalEntities(context, "targets"))
                                    result.Add(SourceOf(context).Facing(target.Source, anchor));
                                return result;
                            }))))
                .Then(Arg<Coordinates>("pos", Vec3Argument.Vec3()).Redirect(execute, context
                    => SourceOf(context).Facing(Vec3Argument.GetVec3(context, "pos")))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("align")
                .Then(Arg<HashSet<Direction.Axis>>("axes", SwizzleArgument.Swizzle()).Redirect(execute, context =>
                {
                    var source = SourceOf(context);
                    return source.WithPosition(source.Position.Align(SwizzleArgument.GetSwizzle(context, "axes")));
                })))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("anchored")
                .Then(Arg<EntityAnchorArgument.Anchor>("anchor", EntityAnchorArgument.EntityAnchor()).Redirect(execute,
                    context => SourceOf(context).WithAnchor(EntityAnchorArgument.GetAnchor(context, "anchor")))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("in")
                .Then(Arg<Identifier>("dimension", DimensionArgument.Dimension()).Redirect(execute, context
                    => SourceOf(context).WithLevel(DimensionArgument.GetDimension(context, "dimension")))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("summon")
                .Then(Arg<Identifier>("entity", new ResourceArgument(Registries.ENTITY_TYPE.Identifier)).Redirect(execute,
                    context => SpawnEntityAndRedirect(SourceOf(context), GetSummonableEntityType(context, "entity")))))
            .Then(BuildRelations(execute, LiteralArgumentBuilder<CommandSourceStack>.Literal("on")));

        dispatcher.Register(root);
    }

    //SourceOf casts the chain source to the server source; every production source is one
    private static ServerCommandSource SourceOf(CommandContext<CommandSourceStack> context)
        => context.GetSource() as ServerCommandSource
           ?? throw new InvalidOperationException("/execute requires a server command source");

    //GetSummonableEntityType resolves the entity type by id, maps to vanilla ResourceArgument.getSummonableEntityType
    private static EntityType<object> GetSummonableEntityType(CommandContext<CommandSourceStack> context, string name)
    {
        var id = ResourceArgument.GetResource(context, name);
        return BuiltInRegistries.ENTITY_TYPE.GetValue(id) as EntityType<object>
               ?? throw ResourceArgument.ErrorResourceNotFound.Create(id);
    }

    //SpawnEntityAndRedirect creates the entity at the source position and keeps executing as it, maps to vanilla spawnEntityAndRedirect
    private static ServerCommandSource SpawnEntityAndRedirect(ServerCommandSource source, EntityType<object> type)
    {
        var level = source.Level;
        if (level is null) return source;
        if (type.Create(level) is not { } entity) return source;
        var (yaw, pitch) = source.Rotation;
        entity.SetPos(source.Position, yaw, pitch);
        level.AddEntity(entity);
        return source.WithEntity(entity).WithLevel(level);
    }

    private static RequiredArgumentBuilder<CommandSourceStack, T> Arg<T>(string name, ArgumentType<T> type)
        => RequiredArgumentBuilder<CommandSourceStack, T>.Argument(name, type);
}
