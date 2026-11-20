using NacreCraft.Game.Server;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.Players;

//ServerOpList operator list in the vanilla shape, maps to vanilla net.minecraft.server.players.ServerOpList
//The storage and persistence live in the existing OpList; this view exposes the NameAndId-shaped entries used by the
//management layer and the old-file converter
public class ServerOpList : OpList
{
    public ServerOpList(string path) : base(path)
    {
    }

    //FindEntry looks up one operator record in the vanilla shape
    public ServerOpListEntry? FindEntry(GameProfile profile)
    {
        foreach (var entry in Entries)
            if (entry.Id == profile.Id || string.Equals(entry.Name, profile.Name, StringComparison.OrdinalIgnoreCase))
                return ToEntry(entry);
        return null;
    }

    //VanillaEntries every operator record in the vanilla shape
    public IReadOnlyList<ServerOpListEntry> VanillaEntries => Entries.Select(ToEntry).ToList();

    private static ServerOpListEntry ToEntry(OpEntry entry)
        => new(NameAndId.From(entry.Id, entry.Name), (PermissionLevel)entry.Level, entry.BypassesPlayerLimit);
}
