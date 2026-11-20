using System.Text.Json;

namespace NacreCraft.Game.Server.Players;

//StoredUserEntry one entry of a stored user list, maps to vanilla net.minecraft.server.players.StoredUserEntry
//The user value is generic (a NameAndId, an address string or a plain name); subclasses add the expiry and serialization fields
public abstract class StoredUserEntry<T>
{
    protected StoredUserEntry(T user) => User = user;

    //User the entry owner, maps to vanilla getUser
    public T User { get; }

    public T GetUser() => User;

    //HasExpired whether the entry is past its expiry, false for entries that never expire
    public virtual bool HasExpired() => false;

    //Serialize writes the entry fields to a JSON object, maps to vanilla serialize
    public abstract void Serialize(Utf8JsonWriter writer);
}
