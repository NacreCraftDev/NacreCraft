using NacreCraft.Game.Server;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level;

//LocalMobCapCalculator per-player local mob caps around a chunk, maps to vanilla net.minecraft.world.level.LocalMobCapCalculator
//ChunkMap has no getPlayersCloseForSpawning, so the server injects that lookup through PlayersNearChunkProvider
//Without a provider no player is near any chunk and every local cap passes
public sealed class LocalMobCapCalculator
{
    private readonly Dictionary<long, IReadOnlyList<ServerPlayer>> _playersNearChunk = new();
    private readonly Dictionary<ServerPlayer, MobCounts> _playerMobCounts = new();

    public ChunkMap ChunkMap { get; }

    public Func<ChunkPos, IReadOnlyList<ServerPlayer>>? PlayersNearChunkProvider { get; set; }

    public LocalMobCapCalculator(ChunkMap chunkMap) => ChunkMap = chunkMap;

    public void AddMob(ChunkPos pos, MobCategory category)
    {
        foreach (var player in GetPlayersNear(pos))
            GetOrCreateCounts(player).Add(category);
    }

    public bool CanSpawn(MobCategory mobCategory, ChunkPos pos)
    {
        foreach (var player in GetPlayersNear(pos))
        {
            if (!_playerMobCounts.TryGetValue(player, out var counts) || counts.CanSpawn(mobCategory))
                return true;
        }
        return false;
    }

    private IReadOnlyList<ServerPlayer> GetPlayersNear(ChunkPos pos)
    {
        var key = pos.Pack();
        if (_playersNearChunk.TryGetValue(key, out var cached)) return cached;
        var players = PlayersNearChunkProvider?.Invoke(pos) ?? Array.Empty<ServerPlayer>();
        _playersNearChunk[key] = players;
        return players;
    }

    private MobCounts GetOrCreateCounts(ServerPlayer player)
    {
        if (_playerMobCounts.TryGetValue(player, out var counts)) return counts;
        counts = new MobCounts();
        _playerMobCounts[player] = counts;
        return counts;
    }

    //MaxInstancesPerChunk vanilla per-category caps, the Registry MobCategory enum carries no such value
    private static int MaxInstancesPerChunk(MobCategory category) => category switch
    {
        MobCategory.Monster => 70,
        MobCategory.Creature => 10,
        MobCategory.Ambient => 15,
        MobCategory.Axolotls => 5,
        MobCategory.UndergroundWaterCreature => 5,
        MobCategory.WaterCreature => 5,
        MobCategory.WaterAmbient => 20,
        _ => -1,
    };

    //MobCounts how many mobs of each category one player already has nearby, maps to vanilla LocalMobCapCalculator.MobCounts
    private sealed class MobCounts
    {
        private readonly Dictionary<MobCategory, int> _counts = new();

        public void Add(MobCategory category)
            => _counts[category] = _counts.GetValueOrDefault(category) + 1;

        public bool CanSpawn(MobCategory category)
            => _counts.GetValueOrDefault(category) < MaxInstancesPerChunk(category);
    }
}
