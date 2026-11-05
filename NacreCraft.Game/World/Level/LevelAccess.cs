using NacreCraft.Game.Server;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Dimension;
using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Primitives.Phys;
using NacreCraft.Registry;
using NacreCraft.Registry.Flag;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Light;
using NacreCraft.Storage.Ticks;
using NacreCraft.Util.Random;
using RegEntity = NacreCraft.Registry.Entity;
using GameDimensionType = NacreCraft.Game.World.Level.LevelGen.Dimension.DimensionType;
using Player = NacreCraft.Game.World.Entity.Player;

namespace NacreCraft.Game.World.Level;

//BlockAndLightGetter level view with light queries, maps to vanilla net.minecraft.world.level.BlockAndLightGetter
//Extends the storage BlockGetter; the light engine sits in Storage so the default queries can be real
public interface BlockAndLightGetter : BlockGetter
{
    LevelLightEngine GetLightEngine();

    //GetBrightness light value of one layer, maps to vanilla getBrightness
    int GetBrightness(LightLayer layer, BlockPos pos)
        => GetLightEngine().GetLayerListener(layer).GetLightValue(pos);

    //GetRawBrightness max of block and sky light after sky darkening, maps to vanilla getRawBrightness
    int GetRawBrightness(BlockPos pos, int darkening)
        => GetLightEngine().GetRawBrightness(pos, darkening);

    //CanSeeSky whether the sky light reaches 15, maps to vanilla canSeeSky
    bool CanSeeSky(BlockPos pos) => GetBrightness(LightLayer.Sky, pos) >= 15;
}

//SignalGetter redstone signal reads, maps to vanilla net.minecraft.world.level.SignalGetter
//Vanilla defaults read through BlockState signal methods; those live on IBlockSignalBehaviour in this project, so only the
//entry reads stay abstract and the traversal defaults are built on them
public interface SignalGetter : BlockGetter
{
    static readonly Direction[] Directions = Direction.Values;

    //GetSignal signal the pos outputs toward the direction, maps to vanilla getSignal
    int GetSignal(BlockPos pos, Direction direction);

    //GetDirectSignal direct signal a conducting block propagates, maps to vanilla getDirectSignal
    int GetDirectSignal(BlockPos pos, Direction direction);

    //GetDirectSignalTo max of the six directions' direct signals, returning early at 15, maps to vanilla getDirectSignalTo
    int GetDirectSignalTo(BlockPos pos)
    {
        var signal = Math.Max(0, GetDirectSignal(pos.Offset(Direction.Down), Direction.Down));
        if (signal >= 15) return signal;
        signal = Math.Max(signal, GetDirectSignal(pos.Offset(Direction.Up), Direction.Up));
        if (signal >= 15) return signal;
        signal = Math.Max(signal, GetDirectSignal(pos.Offset(Direction.North), Direction.North));
        if (signal >= 15) return signal;
        signal = Math.Max(signal, GetDirectSignal(pos.Offset(Direction.South), Direction.South));
        if (signal >= 15) return signal;
        signal = Math.Max(signal, GetDirectSignal(pos.Offset(Direction.West), Direction.West));
        if (signal >= 15) return signal;
        return Math.Max(signal, GetDirectSignal(pos.Offset(Direction.East), Direction.East));
    }

    //GetControlInputSignal reads a diode or signal source input; the block behaviour layer is not reachable here, so it answers 0
    //Maps to vanilla getControlInputSignal, degraded until block states carry signal source queries
    int GetControlInputSignal(BlockPos pos, Direction direction, bool onlyDiodes) => 0;

    bool HasSignal(BlockPos pos, Direction direction) => GetSignal(pos, direction) > 0;

    int GetBestNeighborSignal(BlockPos pos)
    {
        var best = 0;
        foreach (var direction in Directions)
        {
            var signal = GetSignal(pos.Offset(direction), direction);
            if (signal >= 15) return 15;
            if (signal > best) best = signal;
        }
        return best;
    }

    int GetBestOwnOrNeighbourSignal(BlockPos pos) => GetBestNeighborSignal(pos);

    bool HasNeighborSignal(BlockPos pos)
    {
        foreach (var direction in Directions)
            if (GetSignal(pos.Offset(direction), direction) > 0) return true;
        return false;
    }
}

//EntityGetter entity lookup view, maps to vanilla net.minecraft.world.level.EntityGetter
//The generic EntityTypeTest overloads are omitted; the registry entity type is generic and has no wildcard view yet
public interface EntityGetter
{
    //GetEntities entities intersecting the box, except is skipped, maps to vanilla getEntities
    IReadOnlyList<RegEntity> GetEntities(RegEntity? except, AABB bb, Predicate<RegEntity>? selector);

    //Players all players in this view, maps to vanilla players
    IReadOnlyList<Player> Players();

    IReadOnlyList<RegEntity> GetEntities(RegEntity? except, AABB bb)
        => GetEntities(except, bb, null);

    //GetEntitiesOfClass filters the box result by runtime type, maps to vanilla getEntitiesOfClass
    IReadOnlyList<T> GetEntitiesOfClass<T>(AABB bb, Predicate<T>? selector) where T : RegEntity
    {
        var result = new List<T>();
        foreach (var entity in GetEntities(null, bb, null))
        {
            if (entity is T typed && (selector is null || selector(typed)))
                result.Add(typed);
        }
        return result;
    }

    //IsUnobstructed and GetEntityCollisions are omitted: the entity collision layer is not wired into a world view yet
    //Maps to vanilla isUnobstructed / getEntityCollisions

    //GetNearestPlayer nearest matching player within the range, negative range means unlimited, maps to vanilla getNearestPlayer
    Player? GetNearestPlayer(double x, double y, double z, double range, Predicate<RegEntity>? predicate)
    {
        var best = -1.0;
        Player? result = null;
        var target = new Vec3(x, y, z);
        foreach (var player in Players())
        {
            if (predicate is not null && !predicate(player)) continue;
            var distance = player.Pos.DistanceToSqr(target);
            if (range >= 0 && distance >= range * range) continue;
            if (best != -1 && distance >= best) continue;
            best = distance;
            result = player;
        }
        return result;
    }

    Player? GetNearestPlayer(RegEntity source, double maxDist)
        => GetNearestPlayer(source.Pos.X, source.Pos.Y, source.Pos.Z, maxDist, null);

    //FilterOutCreative excludes creative and spectator, maps to vanilla NO_CREATIVE_OR_SPECTATOR
    Player? GetNearestPlayer(double x, double y, double z, double maxDist, bool filterOutCreative)
    {
        Predicate<RegEntity>? filter = filterOutCreative
            ? entity => entity is Player player && player.GameMode != GameType.Creative.Id
            : entity => !entity.IsSpectator();
        return GetNearestPlayer(x, y, z, maxDist, filter);
    }

    bool HasNearbyAlivePlayer(double x, double y, double z, double range)
    {
        var target = new Vec3(x, y, z);
        foreach (var player in Players())
        {
            if (player.IsSpectator() || player.IsRemoved) continue;
            var distance = player.Pos.DistanceToSqr(target);
            if (range < 0 || distance < range * range) return true;
        }
        return false;
    }

    Player? GetPlayerByUUID(Guid uuid)
    {
        foreach (var player in Players())
            if (player.Uuid == uuid) return player;
        return null;
    }
}

//LevelReader read-only level contract, maps to vanilla net.minecraft.world.level.LevelReader
//Extends the block, collision and signal views; the environmental attribute and feature flag pieces of the vanilla bases are not ported
public interface LevelReader : BlockAndLightGetter, CollisionGetter, SignalGetter
{
    //GetChunk chunk by position and status, maps to vanilla getChunk(chunkX, chunkZ, targetStatus, loadOrGenerate)
    ChunkAccess? GetChunk(int chunkX, int chunkZ, ChunkStatus targetStatus, bool loadOrGenerate);

    bool HasChunk(int chunkX, int chunkZ);

    //GetHeight heightmap value of one column, maps to vanilla getHeight
    int GetHeight(Heightmap.Types type, int x, int z);

    int GetSkyDarken();

    BiomeManager GetBiomeManager();

    //GetUncachedNoiseBiome biome sample without the chunk palette, maps to vanilla getUncachedNoiseBiome
    Holder<Biome> GetUncachedNoiseBiome(int quartX, int quartY, int quartZ);

    bool IsClientSide { get; }

    int GetSeaLevel();

    //DimensionType the dimension definition, maps to vanilla dimensionType
    GameDimensionType GetDimensionType();

    RegistryAccess GetRegistryAccess();

    FeatureFlagSet EnabledFeatures();

    int GetHeight(Heightmap.Types type, BlockPos pos) => GetHeight(type, pos.X, pos.Z);

    //GetBiome biome at a block position; vanilla answers a holder, the biome manager answers the value, so the value form is kept
    Biome GetBiome(BlockPos pos) => GetBiomeManager().GetBiome(pos.X, pos.Y, pos.Z);

    BlockState GetBlockState(BlockPos pos) => GetBlockState(pos.X, pos.Y, pos.Z);

    //GetNoiseBiome prefers the BIOMES-stage chunk palette and falls back to a direct climate sample, maps to vanilla getNoiseBiome
    Holder<Biome> GetNoiseBiome(int quartX, int quartY, int quartZ)
    {
        var chunk = GetChunk(quartX >> QuartPos.Bits, quartZ >> QuartPos.Bits, ChunkStatus.BIOMES, false);
        return chunk is not null
            ? chunk.GetNoiseBiome(quartX, quartY, quartZ)
            : GetUncachedNoiseBiome(quartX, quartY, quartZ);
    }

    int GetMinY() => GetDimensionType().MinY;

    int GetMaxY() => GetDimensionType().MaxY;

    int GetHeight() => GetDimensionType().Height;

    BlockPos GetHeightmapPos(Heightmap.Types type, BlockPos pos)
        => new(pos.X, GetHeight(type, pos.X, pos.Z), pos.Z);

    bool IsEmptyBlock(BlockPos pos) => GetBlockState(pos).Owner.IsAir;

    bool IsWaterAt(BlockPos pos) => GetBlockState(pos).FluidState.IsWater;

    //GetMaxLocalRawBrightness raw brightness with the level's sky darkening, maps to vanilla getMaxLocalRawBrightness
    int GetMaxLocalRawBrightness(BlockPos pos) => GetMaxLocalRawBrightness(pos, GetSkyDarken());

    int GetMaxLocalRawBrightness(BlockPos pos, int skyDarkening)
    {
        if (pos.X < -30000000 || pos.Z < -30000000 || pos.X >= 30000000 || pos.Z >= 30000000)
            return 15;
        return GetRawBrightness(pos, skyDarkening);
    }

    int GetEffectiveSkyBrightness(BlockPos pos) => GetBrightness(LightLayer.Sky, pos) - GetSkyDarken();

    ChunkAccess? GetChunk(BlockPos pos)
        => GetChunk(SectionPos.BlockToSectionCoord(pos.X), SectionPos.BlockToSectionCoord(pos.Z), ChunkStatus.FULL, true);

    ChunkAccess? GetChunk(int chunkX, int chunkZ)
        => GetChunk(chunkX, chunkZ, ChunkStatus.FULL, true);

    ChunkAccess? GetChunk(int chunkX, int chunkZ, ChunkStatus status)
        => GetChunk(chunkX, chunkZ, status, true);

    //GetChunkForCollisions is omitted because ChunkAccess has no BlockGetter view in this project
    //Maps to vanilla LevelReader.getChunkForCollisions

    bool HasChunkAt(BlockPos pos)
        => HasChunk(SectionPos.BlockToSectionCoord(pos.X), SectionPos.BlockToSectionCoord(pos.Z));

    bool HasChunksAt(BlockPos pos0, BlockPos pos1)
        => HasChunksAt(pos0.X, pos0.Y, pos0.Z, pos1.X, pos1.Y, pos1.Z);

    bool HasChunksAt(int x0, int y0, int z0, int x1, int y1, int z1)
    {
        if (y1 < GetMinY() || y0 > GetMaxY()) return false;
        return HasChunksAt(x0, z0, x1, z1);
    }

    bool HasChunksAt(int x0, int z0, int x1, int z1)
    {
        for (var chunkX = SectionPos.BlockToSectionCoord(x0); chunkX <= SectionPos.BlockToSectionCoord(x1); chunkX++)
        for (var chunkZ = SectionPos.BlockToSectionCoord(z0); chunkZ <= SectionPos.BlockToSectionCoord(z1); chunkZ++)
        {
            if (!HasChunk(chunkX, chunkZ)) return false;
        }
        return true;
    }
}

//LevelSimulatedReader read side used by feature and placement predicates, maps to vanilla net.minecraft.world.level.LevelSimulatedReader
//GetBlockEntity is omitted because the block entity type view is not exposed through this contract
public interface LevelSimulatedReader
{
    bool IsStateAtPosition(BlockPos pos, Predicate<BlockState> predicate);

    bool IsFluidAtPosition(BlockPos pos, Predicate<FluidState> predicate);

    BlockPos GetHeightmapPos(Heightmap.Types type, BlockPos pos);
}

//LevelWriter write side used by features, maps to vanilla net.minecraft.world.level.LevelWriter
public interface LevelWriter
{
    const int DefaultUpdateLimit = 512;

    bool SetBlock(BlockPos pos, BlockState blockState, int updateFlags, int updateLimit);

    bool RemoveBlock(BlockPos pos, bool movedByPiston);

    bool DestroyBlock(BlockPos pos, bool dropResources, RegEntity? breaker, int updateLimit);

    bool SetBlock(BlockPos pos, BlockState blockState, int updateFlags)
        => SetBlock(pos, blockState, updateFlags, DefaultUpdateLimit);

    bool DestroyBlock(BlockPos pos, bool dropResources)
        => DestroyBlock(pos, dropResources, null, DefaultUpdateLimit);

    bool DestroyBlock(BlockPos pos, bool dropResources, RegEntity? breaker)
        => DestroyBlock(pos, dropResources, breaker, DefaultUpdateLimit);

    bool AddFreshEntity(RegEntity entity) => false;
}

//LevelSimulatedRW combined read and write view, maps to vanilla net.minecraft.world.level.LevelSimulatedRW
public interface LevelSimulatedRW : LevelSimulatedReader, LevelWriter
{
}

//CommonLevelAccessor view shared by generation and the live level, maps to vanilla net.minecraft.world.level.CommonLevelAccessor
//The vanilla explicit overrides only disambiguate identical base members; the C# defaults need no such re-declaration
public interface CommonLevelAccessor : LevelReader, LevelSimulatedRW, EntityGetter
{
}

//ScheduledTickAccess scheduled tick entry point, maps to vanilla net.minecraft.world.level.ScheduledTickAccess
//LevelTickAccess is named ILevelTickAccess in Storage
public interface ScheduledTickAccess
{
    ScheduledTick<T> CreateTick<T>(BlockPos pos, T type, int tickDelay, TickPriority priority) where T : class;

    ScheduledTick<T> CreateTick<T>(BlockPos pos, T type, int tickDelay) where T : class;

    ILevelTickAccess<NacreCraft.Registry.Block> GetBlockTicks();

    ILevelTickAccess<NacreCraft.Registry.Fluid> GetFluidTicks();

    void ScheduleTick(BlockPos pos, NacreCraft.Registry.Block type, int tickDelay, TickPriority priority)
        => GetBlockTicks().Schedule(CreateTick(pos, type, tickDelay, priority));

    void ScheduleTick(BlockPos pos, NacreCraft.Registry.Block type, int tickDelay)
        => GetBlockTicks().Schedule(CreateTick(pos, type, tickDelay));

    void ScheduleTick(BlockPos pos, NacreCraft.Registry.Fluid type, int tickDelay, TickPriority priority)
        => GetFluidTicks().Schedule(CreateTick(pos, type, tickDelay, priority));

    void ScheduleTick(BlockPos pos, NacreCraft.Registry.Fluid type, int tickDelay)
        => GetFluidTicks().Schedule(CreateTick(pos, type, tickDelay));
}

//LevelAccessor mutable level contract, maps to vanilla net.minecraft.world.level.LevelAccessor
//Particle dispatch, sound events and game events keep the shapes the server level already exposes
public interface LevelAccessor : CommonLevelAccessor, ScheduledTickAccess
{
    long NextSubTickCount();

    LevelData GetLevelData();

    MinecraftServer GetServer();

    ChunkSource GetChunkSource();

    RandomSource Random { get; }

    void PlaySound(RegEntity? except, BlockPos pos, SoundEvent sound, SoundSource source, float volume, float pitch);

    void LevelEvent(RegEntity? source, int type, BlockPos pos, int data);

    void GameEvent(NacreCraft.Registry.GameEvent gameEvent, Vec3 position, NacreCraft.Registry.GameEvent.Context context);

    //Neighbor updates and shape updates are handled by the block update sink in Storage, so they are omitted here
    //Maps to vanilla updateNeighborsAt / neighborShapeChanged

    ScheduledTick<T> CreateTick<T>(BlockPos pos, T type, int tickDelay, TickPriority priority) where T : class
        => new(type, pos, GetGameTime() + tickDelay, priority, NextSubTickCount());

    ScheduledTick<T> CreateTick<T>(BlockPos pos, T type, int tickDelay) where T : class
        => new(type, pos, GetGameTime() + tickDelay, TickPriority.Normal, NextSubTickCount());

    long GetGameTime() => GetLevelData().GameTime;

    Difficulty GetDifficulty() => Difficulty.ByName(GetLevelData().DifficultyName) ?? Difficulty.Normal;

    bool HasChunk(int chunkX, int chunkZ) => GetChunkSource().HasChunk(chunkX, chunkZ);

    void PlaySound(RegEntity? except, BlockPos pos, SoundEvent sound, SoundSource source)
        => PlaySound(except, pos, sound, source, 1.0f, 1.0f);

    void LevelEvent(int type, BlockPos pos, int data) => LevelEvent(null, type, pos, data);

    void GameEvent(RegEntity? source, NacreCraft.Registry.GameEvent gameEvent, Vec3 pos)
        => GameEvent(gameEvent, pos, NacreCraft.Registry.GameEvent.Context.Of(source, null));

    void GameEvent(RegEntity? source, NacreCraft.Registry.GameEvent gameEvent, BlockPos pos)
        => GameEvent(gameEvent, new Vec3(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5),
            NacreCraft.Registry.GameEvent.Context.Of(source, null));
}

//ServerLevelAccessor level access owned by a server level, maps to vanilla net.minecraft.world.level.ServerLevelAccessor
//getCurrentDifficultyAt is omitted because DifficultyInstance is not ported
public interface ServerLevelAccessor : LevelAccessor
{
    ServerLevel GetLevel();

    //GetEnvironmentAttributes the environment attribute reader of the level, maps to vanilla environmentAttributes
    //Until a level exposes its attribute system the empty reader answers the registered defaults
    NacreCraft.Registry.Environment.EnvironmentAttributeReader GetEnvironmentAttributes()
        => NacreCraft.Registry.Environment.EnvironmentAttributeReader.Empty;

    //AddFreshEntityWithPassengers adds the entity; passengers are not modelled on the entity yet, so only the root is added
    //Maps to vanilla addFreshEntityWithPassengers, degraded until entity passengers land
    void AddFreshEntityWithPassengers(RegEntity entity) => AddFreshEntity(entity);
}

//WorldGenLevel level view used during chunk generation, maps to vanilla net.minecraft.world.level.WorldGenLevel
public interface WorldGenLevel : ServerLevelAccessor
{
    long GetSeed();

    bool EnsureCanWrite(BlockPos pos) => true;

    void SetCurrentlyGenerating(Func<string>? currentlyGenerating)
    {
    }
}
