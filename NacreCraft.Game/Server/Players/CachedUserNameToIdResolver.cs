using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using NacreCraft.Codec;
using NacreCraft.Logging;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Players;

//CachedUserNameToIdResolver caches name/uuid pairs in usercache.json, maps to vanilla net.minecraft.server.players.CachedUserNameToIdResolver
//Entries expire one month after they are added and the file keeps the thousand most recently used ones. NC has no profile
//repository, so the online lookup is injected as a delegate; a failed lookup falls back to the offline name-derived profile
public sealed class CachedUserNameToIdResolver : UserNameToIdResolver
{
    private const int MaxCachedProfiles = 1000;
    private static readonly string DateFormat = "yyyy-MM-dd HH:mm:ss zzz";

    private readonly string _file;
    private readonly Func<string, NameAndId?> _lookup;
    private readonly ConcurrentDictionary<string, ProfileInfo> _byName = new();
    private readonly ConcurrentDictionary<Guid, ProfileInfo> _byUuid = new();
    private long _operationCount;
    private volatile bool _resolveOfflineUsers = true;

    public CachedUserNameToIdResolver(string file, Func<string, NameAndId?> lookup)
    {
        _file = file;
        _lookup = lookup;
        Load();
    }

    public void ResolveOfflineUsers(bool value) => _resolveOfflineUsers = value;

    public void Add(NameAndId nameAndId) => AddInternal(nameAndId);

    public Optional<NameAndId> Get(string name)
    {
        var key = name.ToLowerInvariant();
        ProfileInfo? info = _byName.TryGetValue(key, out var cached) ? cached : null;
        if (info is not null && info.Expires <= DateTimeOffset.UtcNow)
        {
            _byName.TryRemove(key, out _);
            _byUuid.TryRemove(info.NameAndId.Uuid, out _);
            Save();
            info = null;
        }
        if (info is not null)
        {
            info.LastAccess = Interlocked.Increment(ref _operationCount);
            return Optional<NameAndId>.Of(info.NameAndId);
        }
        var resolved = _lookup(key);
        if (resolved is null && _resolveOfflineUsers) resolved = NameAndId.CreateOffline(name);
        if (resolved is null) return Optional<NameAndId>.Empty();
        var added = AddInternal(resolved);
        return Optional<NameAndId>.Of(added.NameAndId);
    }

    public Optional<NameAndId> Get(Guid id)
    {
        if (!_byUuid.TryGetValue(id, out var info)) return Optional<NameAndId>.Empty();
        info.LastAccess = Interlocked.Increment(ref _operationCount);
        return Optional<NameAndId>.Of(info.NameAndId);
    }

    //Save writes the most recently used profiles, maps to vanilla save
    public void Save()
    {
        try
        {
            var profiles = _byUuid.Values.OrderByDescending(info => info.LastAccess).Take(MaxCachedProfiles).ToList();
            var array = new JsonArray();
            foreach (var info in profiles)
            {
                array.Add(new JsonObject
                {
                    ["name"] = info.NameAndId.Name,
                    ["uuid"] = info.NameAndId.Id,
                    ["expiresOn"] = info.Expires.ToString(DateFormat, CultureInfo.InvariantCulture),
                });
            }
            var directory = Path.GetDirectoryName(_file);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(_file, array.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to save profile cache {_file}: {e.Message}");
        }
    }

    private ProfileInfo AddInternal(NameAndId nameAndId)
    {
        var info = new ProfileInfo(nameAndId, DateTimeOffset.UtcNow.AddMonths(1),
            Interlocked.Increment(ref _operationCount));
        _byName[nameAndId.Name.ToLowerInvariant()] = info;
        if (nameAndId.Uuid != Guid.Empty) _byUuid[nameAndId.Uuid] = info;
        Save();
        return info;
    }

    private void Load()
    {
        if (!File.Exists(_file)) return;
        try
        {
            if (JsonNode.Parse(File.ReadAllText(_file)) is not JsonArray array) return;
            foreach (var node in array)
            {
                if (node is not JsonObject objectNode) continue;
                var name = objectNode["name"]?.GetValue<string>();
                var id = objectNode["uuid"]?.GetValue<string>();
                var expires = objectNode["expiresOn"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id)) continue;
                if (!DateTimeOffset.TryParseExact(expires, DateFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var expiry)) continue;
                var info = new ProfileInfo(new NameAndId { Name = name, Id = id }, expiry, 0);
                _byName[name.ToLowerInvariant()] = info;
                if (info.NameAndId.Uuid != Guid.Empty) _byUuid[info.NameAndId.Uuid] = info;
            }
            Log.Info($"Profile cache loaded {_file} with {_byName.Count} entries");
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load profile cache {_file}: {e.Message}");
        }
    }

    private sealed class ProfileInfo(NameAndId nameAndId, DateTimeOffset expires, long lastAccess)
    {
        public NameAndId NameAndId { get; } = nameAndId;
        public DateTimeOffset Expires { get; } = expires;
        public long LastAccess { get; set; } = lastAccess;
    }
}
