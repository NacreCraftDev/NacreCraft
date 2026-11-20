using NacreCraft.Game.Server;

namespace NacreCraft.Game.Server.Players;

//StoredUserList persisted user list base, maps to vanilla net.minecraft.server.players.StoredUserList
//NC's StoredJsonList already implements the JSON array load/save strategy; this class exposes the vanilla-named base
//of the list hierarchy so user lists can be expressed in the vanilla shape without duplicating the persistence code
public abstract class StoredUserList<TEntry> : StoredJsonList<TEntry> where TEntry : class
{
    protected StoredUserList(string path) : base(path)
    {
    }
}
