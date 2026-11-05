using System.Security.Cryptography;
using System.Text;
using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Network.Protocol.Common;

namespace NacreCraft.Game.Commands;

//ServerPackCommand serverpack command, maps to vanilla net.minecraft.server.commands.ServerPackCommand
//serverpack push <url> [<uuid> [<hash>]] offers a resource pack to every connected player
//serverpack pop <uuid> withdraws it; the connection layer is reached through PlayerList.BroadcastAll
public static class ServerPackCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var push = LiteralArgumentBuilder<CommandSourceStack>.Literal("push")
            .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument("url", StringArgumentType.String())
                .Executes(context => PushPack(context, Guid.Empty, null))
                .Then(RequiredArgumentBuilder<CommandSourceStack, Guid>.Argument("uuid", UuidArgument.Uuid())
                    .Executes(context => PushPack(context, context.GetArgument<Guid>("uuid"), null))
                    .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument("hash", StringArgumentType.Word())
                        .Executes(context => PushPack(context, context.GetArgument<Guid>("uuid"),
                            StringArgumentType.GetString(context, "hash"))))));

        var pop = LiteralArgumentBuilder<CommandSourceStack>.Literal("pop")
            .Then(RequiredArgumentBuilder<CommandSourceStack, Guid>.Argument("uuid", UuidArgument.Uuid())
                .Executes(context => PopPack(context, context.GetArgument<Guid>("uuid"))));

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("serverpack")
            .Requires(s => s.HasPermission(2))
            .Then(push)
            .Then(pop));
    }

    //PushPack sends the push packet to all connections; an empty id derives one from the url like UUID.nameUUIDFromBytes
    private static int PushPack(CommandContext<CommandSourceStack> context, Guid id, string? hash)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        var url = StringArgumentType.GetString(context, "url");
        var packId = id == Guid.Empty ? NameUuid(url) : id;
        source.Server.PlayerList.BroadcastAll(
            new ClientboundResourcePackPushPacket(packId, url, hash ?? "", false, false, ""));
        return 0;
    }

    //PopPack withdraws the given pack on all connections
    private static int PopPack(CommandContext<CommandSourceStack> context, Guid id)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.Server.PlayerList.BroadcastAll(new ClientboundResourcePackPopPacket(id));
        return 0;
    }

    //NameUuid builds the name-based UUID from the url, maps to UUID.nameUUIDFromBytes
    private static Guid NameUuid(string url)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(url));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash, bigEndian: true);
    }
}
