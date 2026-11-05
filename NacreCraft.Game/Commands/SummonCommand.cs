using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Commands;

//SummonCommand summon command, maps to vanilla net.minecraft.server.commands.SummonCommand
//summon <entity> [<pos> [<nbt>]] creates an instance by the entity type factory, lands it and writes the NBT
//Only types with a factory can be created; player entities have no factory and are rejected like a type that cannot be summoned
public static class SummonCommand
{
    //ErrorFailed the type has no factory or creation returned null, maps to vanilla commands.summon.failed
    private static readonly SimpleCommandExceptionType ErrorFailed =
        new(new LiteralMessage("unable to summon that entity"));

    //ErrorDuplicateUuid the level already holds an entity with the same Uuid, maps to vanilla commands.summon.failed.uuid
    private static readonly SimpleCommandExceptionType ErrorDuplicateUuid =
        new(new LiteralMessage("a entity with that uuid already exists"));

    //ErrorInvalidPosition the landing point is outside the spawnable bounds, maps to vanilla commands.summon.invalidPosition
    private static readonly SimpleCommandExceptionType ErrorInvalidPosition =
        new(new LiteralMessage("invalid position for summoning"));

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var entity = RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument(
                "entity", new ResourceArgument(Registries.ENTITY_TYPE.Identifier))
            .Suggests(SuggestEntityTypes)
            .Executes(context => SpawnEntity(context, null, null));
        var pos = RequiredArgumentBuilder<CommandSourceStack, Coordinates>.Argument("pos", Vec3Argument.Vec3())
            .Executes(context => SpawnEntity(context, Vec3Argument.GetVec3(context, "pos"), null))
            .Then(RequiredArgumentBuilder<CommandSourceStack, CompoundTag>.Argument("nbt", CompoundTagArgument.CompoundTag())
                .Executes(context => SpawnEntity(context, Vec3Argument.GetVec3(context, "pos"),
                    CompoundTagArgument.GetCompoundTag(context, "nbt"))));

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("summon")
            .Requires(s => s.HasPermission(2))
            .Then(entity.Then(pos)));
    }

    //SuggestEntityTypes lists only types with a factory, matching what can actually be spawned
    private static Task<Suggestions> SuggestEntityTypes(CommandContext<CommandSourceStack> context,
        SuggestionsBuilder builder)
    {
        var remaining = builder.Remaining.ToLowerInvariant();
        foreach (var holder in BuiltInRegistries.ENTITY_TYPE.ListElements())
        {
            if (holder.Value.Factory is null) continue;
            if (holder.RegisteredName.StartsWith(remaining, StringComparison.Ordinal))
                builder.Add(holder.RegisteredName);
        }
        return builder.BuildFuture();
    }

    //SpawnEntity creates the entity, optionally reads its NBT and adds it to the level, maps to vanilla createEntity+spawnEntity
    //The NBT is read before the position is applied, matching vanilla snapTo after loadEntityRecursive; mob finalizeSpawn and the peaceful check need a mob category that is not on the type yet
    private static int SpawnEntity(CommandContext<CommandSourceStack> context, Vec3? position, CompoundTag? nbt)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        if (source.Level is not { } level)
        {
            source.SendFailure("summon needs a level context");
            return 0;
        }

        var id = ResourceArgument.GetResource(context, "entity");
        var type = BuiltInRegistries.ENTITY_TYPE.GetOptional(id);
        if (type is null) throw ErrorFailed.Create();

        var pos = position ?? source.Position;
        var block = new BlockPos((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y), (int)Math.Floor(pos.Z));
        if (!ServerLevel.IsInSpawnableBounds(block)) throw ErrorInvalidPosition.Create();

        var entity = type.Create(level);
        if (entity is null) throw ErrorFailed.Create();
        if (nbt is not null) entity.Load(nbt);
        entity.SetPos(pos, entity.YRot, entity.XRot);
        if (!level.AddEntity(entity)) throw ErrorDuplicateUuid.Create();

        source.SendSuccess($"summoned {id}");
        return 1;
    }
}
