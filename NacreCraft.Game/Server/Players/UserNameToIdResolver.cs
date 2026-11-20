using NacreCraft.Codec;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Players;

//UserNameToIdResolver name to profile cache contract, maps to vanilla net.minecraft.server.players.UserNameToIdResolver
//Lookups return an empty Optional when the name or id is unknown; offline resolution may synthesize a name-derived profile
public interface UserNameToIdResolver
{
    void Add(NameAndId nameAndId);

    Optional<NameAndId> Get(string name);

    Optional<NameAndId> Get(Guid id);

    void ResolveOfflineUsers(bool value);

    void Save();
}
