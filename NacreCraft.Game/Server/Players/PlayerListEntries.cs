using NacreCraft.Network.Protocol.Status;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.Players;

//ServerOpListEntry an operator record in the vanilla shape, maps to net.minecraft.server.players.ServerOpListEntry
public sealed record ServerOpListEntry(NameAndId User, PermissionLevel Permissions, bool BypassesPlayerLimit)
{
    public NameAndId GetUser() => User;

    public LevelBasedPermissionSet PermissionSet() => LevelBasedPermissionSet.ForLevel(Permissions);
}

//UserBanListEntry lives in BanListEntry.cs, the class hierarchy shared with BanListEntry<T>

//IpBanListEntry an IP ban record in the vanilla shape, maps to net.minecraft.server.players.IpBanListEntry
public sealed record IpBanListEntry(string Ip, string Reason, string Source, string Expires)
{
    public string GetUser() => Ip;

    public string GetReason() => Reason;

    public string GetSource() => Source;

    public string GetExpires() => Expires;
}
