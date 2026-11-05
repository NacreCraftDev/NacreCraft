using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands;

//SpawnArmorTrimsCommand spawn armor trims command, maps to vanilla net.minecraft.server.commands.SpawnArmorTrimsCommand
//spawn_armor_trims *_lag_my_game spawns every pattern, spawn_armor_trims <pattern> spawns one; the pattern is a trim_pattern registry key
//Degraded: there is no armor stand entity type and no trimmed-equipment display layer, so the tree is registered and execution reports that spawning is unavailable
public static class SpawnArmorTrimsCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("spawn_armor_trims")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("*_lag_my_game")
                .Executes(SpawnAllArmorTrims))
            .Then(RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument(
                    "pattern", new ResourceKeyArgument(Registries.TRIM_PATTERN.Identifier))
                .Executes(SpawnArmorTrim)));
    }

    //SpawnAllArmorTrims would iterate every trim pattern, maps to vanilla spawnAllArmorTrims
    private static int SpawnAllArmorTrims(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //SpawnArmorTrim would spawn the given pattern, maps to vanilla spawnArmorTrim
    private static int SpawnArmorTrim(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //Unavailable resolves the executor so console use still reports the player requirement, then explains the missing subsystem
    private static int Unavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        _ = source.PlayerOrThrow;
        source.SendFailure("armor stand spawning is not implemented on this server; no armor trim was spawned");
        return 0;
    }
}
