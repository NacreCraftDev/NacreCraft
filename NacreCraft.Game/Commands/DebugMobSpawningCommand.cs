using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;

namespace NacreCraft.Game.Commands;

//DebugMobSpawningCommand debug mob spawning command, maps to vanilla net.minecraft.server.commands.DebugMobSpawningCommand
//Degraded: MobCategory and NaturalSpawner.spawnCategoryForPosition are not ported, so the literal names are
//kept in the tree and every branch reports that the debug spawn attempt is unavailable
public static class DebugMobSpawningCommand
{
    //CategoryNames the vanilla mob category names used as literals, maps to MobCategory.values()
    private static readonly string[] CategoryNames =
    {
        "monster", "creature", "ambient", "axolotls", "underground_water_creature",
        "water_creature", "water_ambient", "misc",
    };

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var root = LiteralArgumentBuilder<CommandSourceStack>.Literal("debugmobspawning")
            .Requires(s => s.HasPermission(2));
        foreach (var category in CategoryNames)
            root.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal(category)
                .Then(RequiredArgumentBuilder<CommandSourceStack, BlockCoordinates>.Argument(
                        "at", BlockPosArgument.BlockPos())
                    .Executes(context => SpawnMobs(context, category))));
        dispatcher.Register(root);
    }

    //SpawnMobs would run a single spawn attempt at the position, maps to vanilla spawnMobs
    private static int SpawnMobs(CommandContext<CommandSourceStack> context, string category)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure($"debug mob spawning is not implemented on this server; no {category} spawn was attempted");
        return 0;
    }
}
