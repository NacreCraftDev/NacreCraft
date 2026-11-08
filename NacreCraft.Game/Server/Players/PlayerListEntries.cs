using NacreCraft.Network.Protocol.Status;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.Players;

//ServerOpListEntry an operator record in the vanilla shape, maps to net.minecraft.server.players.ServerOpListEntry
public sealed record ServerOpListEntry(NameAndId User, PermissionLevel Permissions, bool BypassesPlayerLimit)
{
    public NameAndId GetUser() => User;

    public LevelBasedPermissionSet PermissionSet() => LevelBasedPermissionSet.ForLevel(Permissions);
}

//UserBanListEntry a player ban record in the vanilla shape, maps to net.minecraft.server.players.UserBanListEntry
//The game-side list persists BanEntry with string timestamps, so this adapter keeps the same string form
public sealed record UserBanListEntry(NameAndId User, string? DisplayName, string Reason, string Source, string Expires)
{
    public NameAndId GetUser() => User;

    public string GetReason() => Reason;

    public string GetSource() => Source;

    public string GetExpires() => Expires;
}

//IpBanListEntry an IP ban record in the vanilla shape, maps to net.minecraft.server.players.IpBanListEntry
public sealed record IpBanListEntry(string Ip, string Reason, string Source, string Expires)
{
    public string GetUser() => Ip;

    public string GetReason() => Reason;

    public string GetSource() => Source;

    public string GetExpires() => Expires;
}
