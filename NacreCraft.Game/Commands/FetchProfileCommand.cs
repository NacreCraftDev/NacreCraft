using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;

namespace NacreCraft.Game.Commands;

//FetchProfileCommand fetch profile command, maps to vanilla net.minecraft.server.commands.FetchProfileCommand
//fetchprofile name <name> / id <uuid> / entity <target> resolves a game profile and prints copy/give/summon shortcuts
//Degraded: there is no profile resolver (name/uuid lookup) and no player-sprite component or resolved-profile codec, so the tree is registered and execution reports that resolution is unavailable
public static class FetchProfileCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var name = LiteralArgumentBuilder<CommandSourceStack>.Literal("name")
            .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument("name", StringArgumentType.GreedyString())
                .Executes(ResolveName));

        var id = LiteralArgumentBuilder<CommandSourceStack>.Literal("id")
            .Then(RequiredArgumentBuilder<CommandSourceStack, Guid>.Argument("id", UuidArgument.Uuid())
                .Executes(ResolveId));

        var entity = LiteralArgumentBuilder<CommandSourceStack>.Literal("entity")
            .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument("entity", EntityArgument.Entity())
                .Executes(PrintForEntity));

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("fetchprofile")
            .Requires(s => s.HasPermission(2))
            .Then(name)
            .Then(id)
            .Then(entity));
    }

    //ResolveName would look the profile up by name off-thread, maps to vanilla resolveName
    private static int ResolveName(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //ResolveId would look the profile up by uuid off-thread, maps to vanilla resolveId
    private static int ResolveId(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //PrintForEntity would read the avatar profile of the target, maps to vanilla printForEntity
    private static int PrintForEntity(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        EntityArgument.GetSingleTarget(context, "entity");
        source.SendFailure("profile resolution is not implemented on this server; no profile was fetched");
        return 0;
    }

    //Unavailable explains the missing profile resolver without touching the network
    private static int Unavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("profile resolution is not implemented on this server; no profile was fetched");
        return 0;
    }
}
