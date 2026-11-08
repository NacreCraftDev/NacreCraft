using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Players;

//UserWhiteListEntry a whitelist record in the vanilla shape, maps to net.minecraft.server.players.UserWhiteListEntry
//The management RPC carries these entries; the game-side list still persists WhiteListEntry and converts at this boundary
public sealed record UserWhiteListEntry(NameAndId User)
{
    public NameAndId GetUser() => User;
}
