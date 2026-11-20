using NacreCraft.Game.Server;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Players;

//UserWhiteList whitelist in the vanilla shape, maps to vanilla net.minecraft.server.players.UserWhiteList
//The storage and persistence live in the existing WhiteList; this view exposes the NameAndId-shaped entries used by the
//management layer and the old-file converter
public class UserWhiteList : WhiteList
{
    public UserWhiteList(string path) : base(path)
    {
    }

    //FindEntry looks up one whitelist record in the vanilla shape
    public UserWhiteListEntry? FindEntry(GameProfile profile)
    {
        foreach (var entry in Entries)
            if (entry.Id == profile.Id || string.Equals(entry.Name, profile.Name, StringComparison.OrdinalIgnoreCase))
                return new UserWhiteListEntry(NameAndId.From(entry.Id, entry.Name));
        return null;
    }

    //VanillaEntries every whitelist record in the vanilla shape
    public IReadOnlyList<UserWhiteListEntry> VanillaEntries
        => Entries.Select(entry => new UserWhiteListEntry(NameAndId.From(entry.Id, entry.Name))).ToList();
}
