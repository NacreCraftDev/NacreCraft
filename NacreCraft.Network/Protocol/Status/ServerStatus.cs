using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NacreCraft.Config;

namespace NacreCraft.Network.Protocol.Status;

//ServerStatus server status, maps to vanilla net.minecraft.network.protocol.status.ServerStatus
//The simplified form uses System.Text.Json serialization instead of the vanilla Codec system
//Vanilla depends on classes such as Component/Codec/WorldVersion that NacreCraft has not yet implemented
//The simplified form uses string for description instead of Component
public sealed record ServerStatus
{
    //Description the server MOTD description, string in the simplified form
    public string Description { get; init; } = string.Empty;

    //Players player info, optional
    public PlayersData? Players { get; init; }

    //Version version info, optional
    public VersionData? Version { get; init; }

    //Favicon server icon, optional base64 PNG
    public FaviconData? Favicon { get; init; }

    //EnforcesSecureChat whether secure chat is enforced, false by default
    public bool EnforcesSecureChat { get; init; }

    //ToJson serializes to a JSON string for network transport
    public string ToJson() => JsonSerializer.Serialize(this, JsonContext);

    //FromJson deserializes a JSON string into ServerStatus
    public static ServerStatus? FromJson(string json) =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<ServerStatus>(json, JsonContext);

    //JsonContext serialization configuration, ignoring null values and using camelCase
    private static readonly JsonSerializerOptions JsonContext = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    //PlayersData player info, maps to vanilla ServerStatus.Players
    //Uses PlayersData to avoid a name clash with the outer Players property
    //Online writable so the server can periodically refresh the online count
    public sealed record PlayersData
    {
        public int Max { get; init; }
        public int Online { get; set; }
        public List<NameAndId> Sample { get; init; } = new();
    }

    //VersionData version info, maps to vanilla ServerStatus.Version
    //Uses VersionData to avoid a clash with System.Version
    public sealed record VersionData
    {
        public string Name { get; init; } = string.Empty;
        public int Protocol { get; init; }

        //Current returns the current NacreCraft implementation version
        //S4 aligns with 26.2 version.json: name 26.2, protocol 776
        public static VersionData Current() => new()
        {
            Name = SharedConstants.Version,
            Protocol = SharedConstants.ProtocolVersion,
        };
    }

    //FaviconData server icon, maps to vanilla ServerStatus.Favicon
    //Vanilla base64-encodes a PNG; the simplified form stores byte[] directly
    public sealed record FaviconData(byte[] IconBytes)
    {
        //Prefix the base64 data URL prefix
        public const string Prefix = "data:image/png;base64,";

        //ToBase64 encodes into a base64 data URL
        public string ToBase64() => Prefix + Convert.ToBase64String(IconBytes);

        //FromBase64 decodes a base64 data URL
        public static FaviconData? FromBase64(string base64)
        {
            if (!base64.StartsWith(Prefix)) return null;
            try
            {
                var data = Convert.FromBase64String(base64[Prefix.Length..]);
                return new FaviconData(data);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}

//NameAndId simplified player name and UUID, maps to vanilla net.minecraft.server.players.NameAndId
//Vanilla serializes with a Codec list; the simplified form uses a record with JSON
public sealed record NameAndId
{
    public string Name { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;

    //Uuid the parsed uuid, Guid.Empty when the id is missing or malformed
    public Guid Uuid => Guid.TryParse(Id, out var id) ? id : Guid.Empty;

    //From builds a name and id pair from a uuid
    public static NameAndId From(Guid id, string name) => new() { Name = name, Id = id.ToString("D") };

    //CreateOffline builds the offline-mode profile of a name, maps to vanilla NameAndId.createOffline
    //The uuid is UUID.nameUUIDFromBytes("OfflinePlayer:" + name), the same derivation the login flow uses
    public static NameAndId CreateOffline(string name) => From(OfflineUuid(name), name);

    //OfflineUuid the version-3 MD5 uuid of an offline player name
    private static Guid OfflineUuid(string name)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + name));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash, bigEndian: true);
    }
}
