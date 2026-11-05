using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.Commands;

//GiveCommand /give command, maps to vanilla net.minecraft.server.commands.GiveCommand
//What fits goes into the inventory; what does not is dropped at the player's feet by the core; the actual placed count is returned
public static class GiveCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("give")
            .Requires(s => s.HasPermission(2))
            .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument("targets", EntityArgument.Players())
                .Then(RequiredArgumentBuilder<CommandSourceStack, ItemInput>.Argument("item", ItemArgument.Item())
                    .Executes(context => Give(context, 1))
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument("count", IntegerArgumentType.Integer(1))
                        .Executes(context => Give(context, IntegerArgumentType.GetInteger(context, "count")))))));
    }

    //Give uses the core give entry, maps to the vanilla give command's inventory.add plus drop combination
    private static int Give(CommandContext<CommandSourceStack> context, int count)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        var input = ItemArgument.GetItemInput(context, "item");
        var players = EntityArgument.GetPlayers(context, "targets");
        var name = input.Id.ToShortString();
        var given = 0;
        foreach (var player in players)
        {
            var stack = new ItemStack(input.Item.BuiltInRegistryHolder, count, input.Components);
            var placed = player.GiveItem(stack);
            if (placed == 0)
            {
                source.SendFailure($"player {player.Profile.Name}'s inventory is full");
                continue;
            }
            if (ReferenceEquals(player, source.Player))
            {
                source.SendSuccess($"gave {placed} x {name}");
            }
            else
            {
                source.SendSuccess($"gave {player.Profile.Name} {placed} x {name}");
                player.Connection.Send(new ClientboundSystemChatPacket(
                    Component.Literal($"received {placed} x {name}"), false));
            }
            given++;
        }
        return given;
    }
}
