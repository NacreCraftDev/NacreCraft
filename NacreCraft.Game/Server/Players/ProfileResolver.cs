using NacreCraft.Codec;
using NacreCraft.Network.Protocol.Login;

namespace NacreCraft.Game.Server.Players;

//ProfileResolver resolves game profiles by name or id, maps to vanilla net.minecraft.server.players.ProfileResolver
//The online lookup is injected: NC has no session service, so callers wire it to their authentication backend
public interface ProfileResolver
{
    Optional<GameProfile> FetchByName(string name);

    Optional<GameProfile> FetchById(Guid id);

    //Cached resolver caching both directions for ten minutes with a 256 entry cap, maps to vanilla ProfileResolver.Cached
    public sealed class Cached : ProfileResolver
    {
        private const int MaxEntries = 256;
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

        private readonly Func<Guid, GameProfile?> _fetchById;
        private readonly UserNameToIdResolver _nameToId;
        private readonly Dictionary<Guid, (GameProfile Profile, DateTimeOffset Expires)> _byId = new();
        private readonly Dictionary<string, (GameProfile Profile, DateTimeOffset Expires)> _byName =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public Cached(Func<Guid, GameProfile?> fetchById, UserNameToIdResolver nameToId)
        {
            _fetchById = fetchById;
            _nameToId = nameToId;
        }

        public Optional<GameProfile> FetchByName(string name)
        {
            if (!IsValidPlayerName(name)) return Optional<GameProfile>.Empty();
            lock (_lock)
            {
                if (_byName.TryGetValue(name, out var cached) && cached.Expires > DateTimeOffset.UtcNow)
                    return Optional<GameProfile>.Of(cached.Profile);
            }
            var resolved = _nameToId.Get(name);
            if (!resolved.IsPresent) return Optional<GameProfile>.Empty();
            return FetchById(resolved.Get().Uuid);
        }

        public Optional<GameProfile> FetchById(Guid id)
        {
            lock (_lock)
            {
                if (_byId.TryGetValue(id, out var cached) && cached.Expires > DateTimeOffset.UtcNow)
                    return Optional<GameProfile>.Of(cached.Profile);
            }
            var profile = _fetchById(id);
            if (profile is null) return Optional<GameProfile>.Empty();
            var expires = DateTimeOffset.UtcNow + Lifetime;
            lock (_lock)
            {
                EvictExpired();
                _byId[id] = (profile, expires);
                if (!string.IsNullOrEmpty(profile.Name)) _byName[profile.Name] = (profile, expires);
            }
            return Optional<GameProfile>.Of(profile);
        }

        private void EvictExpired()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var key in _byId.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToList())
                _byId.Remove(key);
            foreach (var key in _byName.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToList())
                _byName.Remove(key);
            //Keep the cap hard even when no entries are expired: drop the soonest-expiring ones
            if (_byId.Count > MaxEntries)
                foreach (var key in _byId.OrderBy(pair => pair.Value.Expires).Take(_byId.Count - MaxEntries)
                             .Select(pair => pair.Key).ToList())
                    _byId.Remove(key);
            if (_byName.Count > MaxEntries)
                foreach (var key in _byName.OrderBy(pair => pair.Value.Expires).Take(_byName.Count - MaxEntries)
                             .Select(pair => pair.Key).ToList())
                    _byName.Remove(key);
        }

        //IsValidPlayerName vanilla name rule: 1..16 characters of letters, digits and underscore
        private static bool IsValidPlayerName(string name)
        {
            if (name.Length is < 1 or > 16) return false;
            foreach (var c in name)
                if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            return true;
        }
    }
}
