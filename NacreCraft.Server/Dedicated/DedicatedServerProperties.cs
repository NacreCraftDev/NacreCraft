using NacreCraft;
using NacreCraft.Config;

namespace NacreCraft.Game.Server;

//DedicatedServerProperties the full server.properties schema of the dedicated server
//Maps to vanilla net.minecraft.server.dedicated.DedicatedServerProperties
//ServerSettings already carries the core keys consumed by the command/RPC layer; this class adds the remaining vanilla keys and
//the file load/store helpers, and stays the type the dedicated settings owner hands to the server
public class DedicatedServerProperties : ServerSettings
{
    //PreventProxyConnections rejects logins whose HAProxy header and remote address disagree, maps to prevent-proxy-connections
    public bool PreventProxyConnections => GetBool("prevent-proxy-connections", false);

    //EnableCodeOfConduct shows the code of conduct screen to joining players, maps to enable-code-of-conduct
    public bool EnableCodeOfConduct => GetBool("enable-code-of-conduct", false);

    //BugReportLink the URL shown by the client's bug report screen, maps to bug-report-link
    public string BugReportLink => GetOrDefault("bug-report-link", string.Empty);

    //Hardcore locks the server to hardcore mode, maps to hardcore
    public bool Hardcore => GetBool("hardcore", false);

    //UseNativeTransport prefers native transport when available; NC has no epoll/kqueue path, exposed for parity, maps to use-native-transport
    public bool UseNativeTransport => GetBool("use-native-transport", true);

    //MaxChainedNeighborUpdates caps a single chained neighbor update cascade, maps to max-chained-neighbor-updates
    public int MaxChainedNeighborUpdates => GetInt("max-chained-neighbor-updates", SharedConstants.MaxChainedNeighborUpdates);

    //RateLimitPacketsPerSecond per-connection packet rate limit, 0 disables it, maps to rate-limit
    public int RateLimitPacketsPerSecond => GetInt("rate-limit", 0);

    //CommandSpamThresholdSeconds chat filter threshold for commands, maps to command-spam-threshold-seconds
    public int CommandSpamThresholdSeconds => GetInt("command-spam-threshold-seconds", 10);

    //ChatSpamThresholdSeconds chat filter threshold for chat, maps to chat-spam-threshold-seconds
    public int ChatSpamThresholdSeconds => GetInt("chat-spam-threshold-seconds", 10);

    //NetworkCompressionThreshold packet size above which compression starts, -1 disables it, maps to network-compression-threshold
    public int NetworkCompressionThreshold => GetInt("network-compression-threshold", 256);

    //BroadcastConsoleToOps whether console command output is echoed to operators, maps to broadcast-console-to-ops
    public bool BroadcastConsoleToOps => GetBool("broadcast-console-to-ops", true);

    //SyncChunkWrites whether region writes are flushed synchronously, maps to sync-chunk-writes
    public bool SyncChunkWrites => GetBool("sync-chunk-writes", true);

    //RegionFileCompression the region file compression algorithm name, maps to region-file-compression
    public string RegionFileCompression => GetOrDefault("region-file-compression", "deflate");

    //TextFilteringConfig the chat filtering service configuration, maps to text-filtering-config
    public string TextFilteringConfig => GetOrDefault("text-filtering-config", string.Empty);

    //TextFilteringVersion the chat filtering service protocol version, maps to text-filtering-version
    public int TextFilteringVersion => GetInt("text-filtering-version", 0);

    //EnforceSecureProfile whether unsigned chat profiles are rejected, maps to enforce-secure-profile
    public bool EnforceSecureProfile => GetBool("enforce-secure-profile", true);

    //LogIps whether player IP addresses are written to the log, maps to log-ips
    public bool LogIps => GetBool("log-ips", true);

    //GeneratorSettings the flat world generator settings JSON, maps to generator-settings
    public string GeneratorSettings => GetOrDefault("generator-settings", "{}");

    //InitialEnabledPacks the data packs enabled on world creation, maps to initial-enabled-packs
    public string InitialEnabledPacks => GetOrDefault("initial-enabled-packs", "vanilla");

    //InitialDisabledPacks the data packs disabled on world creation, maps to initial-disabled-packs
    public string InitialDisabledPacks => GetOrDefault("initial-disabled-packs", string.Empty);

    //ResourcePack the resource pack URL offered to clients, maps to resource-pack
    public string ResourcePack => GetOrDefault("resource-pack", string.Empty);

    //ResourcePackId the stable id of the offered resource pack, maps to resource-pack-id
    public string ResourcePackId => GetOrDefault("resource-pack-id", string.Empty);

    //ResourcePackSha1 the SHA-1 digest of the offered resource pack, maps to resource-pack-sha1
    public string ResourcePackSha1 => GetOrDefault("resource-pack-sha1", string.Empty);

    //ResourcePackPrompt the prompt text shown before applying the pack, maps to resource-pack-prompt
    public string ResourcePackPrompt => GetOrDefault("resource-pack-prompt", string.Empty);

    //RequireResourcePack whether the pack is mandatory, maps to require-resource-pack
    public bool RequireResourcePack => GetBool("require-resource-pack", false);

    //FromFile loads the properties file, maps to vanilla DedicatedServerProperties.fromFile
    public static DedicatedServerProperties FromFile(string file)
    {
        var properties = new DedicatedServerProperties();
        properties.Load(file);
        return properties;
    }

    //Store writes the current properties back to the file, maps to vanilla DedicatedServerProperties.store
    public void Store(string file) => Save(file);

    //DefaultEntries appends the keys this class adds above the core ServerSettings list
    //A generated default file and an old file being filled therefore both carry the full vanilla key set
    protected override IEnumerable<(string Key, string Value)> DefaultEntries()
    {
        foreach (var entry in base.DefaultEntries()) yield return entry;
        yield return ("prevent-proxy-connections", PreventProxyConnections ? "true" : "false");
        yield return ("enable-code-of-conduct", EnableCodeOfConduct ? "true" : "false");
        yield return ("bug-report-link", BugReportLink);
        yield return ("hardcore", Hardcore ? "true" : "false");
        yield return ("use-native-transport", UseNativeTransport ? "true" : "false");
        yield return ("max-chained-neighbor-updates", MaxChainedNeighborUpdates.ToString());
        yield return ("rate-limit", RateLimitPacketsPerSecond.ToString());
        yield return ("command-spam-threshold-seconds", CommandSpamThresholdSeconds.ToString());
        yield return ("chat-spam-threshold-seconds", ChatSpamThresholdSeconds.ToString());
        yield return ("network-compression-threshold", NetworkCompressionThreshold.ToString());
        yield return ("broadcast-console-to-ops", BroadcastConsoleToOps ? "true" : "false");
        yield return ("sync-chunk-writes", SyncChunkWrites ? "true" : "false");
        yield return ("region-file-compression", RegionFileCompression);
        yield return ("text-filtering-config", TextFilteringConfig);
        yield return ("text-filtering-version", TextFilteringVersion.ToString());
        yield return ("enforce-secure-profile", EnforceSecureProfile ? "true" : "false");
        yield return ("log-ips", LogIps ? "true" : "false");
        yield return ("generator-settings", GeneratorSettings);
        yield return ("initial-enabled-packs", InitialEnabledPacks);
        yield return ("initial-disabled-packs", InitialDisabledPacks);
        yield return ("resource-pack", ResourcePack);
        yield return ("resource-pack-id", ResourcePackId);
        yield return ("resource-pack-sha1", ResourcePackSha1);
        yield return ("resource-pack-prompt", ResourcePackPrompt);
        yield return ("require-resource-pack", RequireResourcePack ? "true" : "false");
    }
}
