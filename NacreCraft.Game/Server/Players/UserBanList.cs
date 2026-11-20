using NacreCraft.Game.Server;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Players;

//UserBanList player ban list in the vanilla shape, maps to vanilla net.minecraft.server.players.UserBanList
//The storage and persistence live in the existing BanList; this view exposes the NameAndId-shaped entries the management
//layer and old-file converter use, converting at the boundary instead of keeping two copies of the list
public class UserBanList : BanList
{
    public UserBanList(string path) : base(path)
    {
    }

    //FindEntry looks up one ban record in the vanilla shape
    public UserBanListEntry? FindEntry(GameProfile profile)
        => Find(profile) is { } entry ? UserBanListEntry.FromBanEntry(entry) : null;

    //VanillaEntries every ban record in the vanilla shape
    public IReadOnlyList<UserBanListEntry> VanillaEntries
        => Entries.Select(UserBanListEntry.FromBanEntry).ToList();
}
