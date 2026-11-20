using System.Globalization;
using System.Text.Json;
using NacreCraft.Game.Server;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Players;

//BanListEntry a ban record with a user, source, timestamps and reason, maps to vanilla net.minecraft.server.players.BanListEntry
//NC persists timestamps as strings; the legacy "yyyy-MM-dd HH:mm:ss Z" format is accepted when reading old files and the
//canonical ISO-8601 form is written back
public abstract class BanListEntry<T> : StoredUserEntry<T>
{
    public const string ExpiresNever = "forever";

    //LegacyDateFormat the old server date format, maps to vanilla DATE_FORMAT
    public const string LegacyDateFormat = "yyyy-MM-dd HH:mm:ss zzz";

    protected BanListEntry(T user, string created, string source, string expires, string reason)
        : base(user)
    {
        Created = string.IsNullOrEmpty(created) ? DateTimeOffset.UtcNow.ToString("o") : created;
        Source = string.IsNullOrEmpty(source) ? "(Unknown)" : source;
        Expires = string.IsNullOrEmpty(expires) ? ExpiresNever : expires;
        Reason = reason;
    }

    public string Created { get; }
    public string Source { get; }
    public string Expires { get; }
    public string Reason { get; }

    //GetCreated/GetSource/GetExpires/GetReason vanilla accessors
    public string GetCreated() => Created;
    public string GetSource() => Source;
    public string GetExpires() => Expires;
    public string GetReason() => Reason;

    //HasExpired whether the expiry time has passed, maps to vanilla hasExpired
    public override bool HasExpired() => ParseInstant(Expires) is { } expires && expires <= DateTimeOffset.UtcNow;

    //ParseInstant reads either an ISO timestamp or the legacy format
    public static DateTimeOffset? ParseInstant(string text)
        => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso)
            ? iso
            : DateTimeOffset.TryParseExact(text, LegacyDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var legacy)
                ? legacy
                : null;

    //Serialize writes the common ban fields; the subclass adds the user fields first
    public override void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteString("created", Created);
        writer.WriteString("source", Source);
        writer.WriteString("expires", Expires);
        if (Reason.Length > 0) writer.WriteString("reason", Reason);
    }
}

//UserBanListEntry a player ban record, maps to vanilla net.minecraft.server.players.UserBanListEntry
//The constructor keeps the game-side argument order (display name, reason, source, expires) used by the RPC adapter and adds
//an optional creation timestamp so the old-file converter can preserve it
public sealed class UserBanListEntry : BanListEntry<NameAndId>
{
    public UserBanListEntry(NameAndId user, string? displayName, string reason, string source, string expires,
        string? created = null)
        : base(user, created ?? "", source, expires, reason)
    {
        DisplayName = displayName;
    }

    public UserBanListEntry(NameAndId user) : base(user, "", "", BanListEntry<NameAndId>.ExpiresNever, "")
    {
    }

    //DisplayName the display name carried by the RPC ban DTO, null when the ban has only a name
    public string? DisplayName { get; }

    //FromBanEntry converts the game-side stored record, used by the vanilla-shaped user ban list
    public static UserBanListEntry FromBanEntry(BanEntry entry)
        => new(NameAndId.From(entry.Id, entry.Name), null, entry.Reason, entry.Source, entry.Expires, entry.Created);

    //Serialize writes the user uuid/name and the common ban fields
    public override void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteString("uuid", User.Id);
        writer.WriteString("name", User.Name);
        base.Serialize(writer);
    }
}
