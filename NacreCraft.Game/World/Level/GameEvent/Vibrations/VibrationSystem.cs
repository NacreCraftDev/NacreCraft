using NacreCraft.Game.Server;
using NacreCraft.Game.World.Particle;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util;
using BlockTags = NacreCraft.Game.World.Level.Block.BlockTags;

namespace NacreCraft.Game.World.Level.GameEvents.Vibrations;

//VibrationSystem the vibration state machine behind sculk sensors, maps to vanilla VibrationSystem
//A listener collects candidates as game events arrive, keeps only the best one per tick, then counts down the
//travel time before handing the vibration to the user; only one vibration is in flight at a time
//Sculk sensors, shriekers, the warden and allays all build on this, none of which exist here yet
public interface VibrationSystem
{
    //ResonanceEvents the fifteen resonate events, one per vibration frequency, maps to vanilla RESONANCE_EVENTS
    public static readonly GameEvent[] ResonanceEvents =
    [
        GameEvent.Resonate1, GameEvent.Resonate2, GameEvent.Resonate3, GameEvent.Resonate4, GameEvent.Resonate5,
        GameEvent.Resonate6, GameEvent.Resonate7, GameEvent.Resonate8, GameEvent.Resonate9, GameEvent.Resonate10,
        GameEvent.Resonate11, GameEvent.Resonate12, GameEvent.Resonate13, GameEvent.Resonate14, GameEvent.Resonate15
    ];

    //NoVibrationFrequency the frequency of an event that does not vibrate, maps to vanilla NO_VIBRATION_FREQUENCY
    public const int NoVibrationFrequency = 0;

    //VibrationFrequencyForEvent the frequency each event resonates at, maps to vanilla VIBRATION_FREQUENCY_FOR_EVENT
    //A sculk sensor compares frequencies to break ties between vibrations of equal distance
    public static readonly Dictionary<GameEvent, int> VibrationFrequencyForEvent = BuildFrequencyTable();

    //VibrationData the per-listener state, maps to vanilla getVibrationData
    Data VibrationData { get; }

    //VibrationUser the host behaviour driving this system, maps to vanilla getVibrationUser
    User VibrationUser { get; }

    //GetGameEventFrequency the frequency of an event, maps to vanilla getGameEventFrequency
    public static int GetGameEventFrequency(GameEvent gameEvent)
        => VibrationFrequencyForEvent.TryGetValue(gameEvent, out var frequency) ? frequency : NoVibrationFrequency;

    //GetResonanceEventByFrequency the resonate event of a frequency, maps to vanilla getResonanceEventByFrequency
    public static GameEvent GetResonanceEventByFrequency(int vibrationFrequency)
        => ResonanceEvents[vibrationFrequency - 1];

    //GetRedstoneStrengthForDistance the signal a sensor emits for a vibration that far away, maps to vanilla method of the same name
    public static int GetRedstoneStrengthForDistance(float distance, int listenerRadius)
    {
        var powerScale = 15.0 / listenerRadius;
        return Math.Max(1, 15 - Mth.Floor(powerScale * distance));
    }

    private static Dictionary<GameEvent, int> BuildFrequencyTable()
    {
        //GameEvent is a value record, so the default comparer would collapse events that share a notification radius
        var map = new Dictionary<GameEvent, int>(ReferenceEqualityComparer.Instance)
        {
            [GameEvent.Step] = 1,
            [GameEvent.Swim] = 1,
            [GameEvent.Flap] = 1,
            [GameEvent.ProjectileLand] = 2,
            [GameEvent.HitGround] = 2,
            [GameEvent.Splash] = 2,
            [GameEvent.Bounce] = 2,
            [GameEvent.ItemInteractFinish] = 3,
            [GameEvent.ProjectileShoot] = 3,
            [GameEvent.InstrumentPlay] = 3,
            [GameEvent.EntityAction] = 4,
            [GameEvent.ElytraGlide] = 4,
            [GameEvent.Unequip] = 4,
            [GameEvent.EntityDismount] = 5,
            [GameEvent.Equip] = 5,
            [GameEvent.EntityInteract] = 6,
            [GameEvent.Shear] = 6,
            [GameEvent.EntityMount] = 6,
            [GameEvent.EntityDamage] = 7,
            [GameEvent.Drink] = 8,
            [GameEvent.Eat] = 8,
            [GameEvent.ContainerClose] = 9,
            [GameEvent.BlockClose] = 9,
            [GameEvent.BlockDeactivate] = 9,
            [GameEvent.BlockDetach] = 9,
            [GameEvent.ContainerOpen] = 10,
            [GameEvent.BlockOpen] = 10,
            [GameEvent.BlockActivate] = 10,
            [GameEvent.BlockAttach] = 10,
            [GameEvent.PrimeFuse] = 10,
            [GameEvent.NoteBlockPlay] = 10,
            [GameEvent.BlockChange] = 11,
            [GameEvent.BlockDestroy] = 12,
            [GameEvent.FluidPickup] = 12,
            [GameEvent.BlockPlace] = 13,
            [GameEvent.FluidPlace] = 13,
            [GameEvent.EntityPlace] = 14,
            [GameEvent.LightningStrike] = 14,
            [GameEvent.Teleport] = 14,
            [GameEvent.EntityDie] = 15,
            [GameEvent.Explode] = 15
        };
        for (var frequency = 1; frequency <= 15; frequency++)
            map[GetResonanceEventByFrequency(frequency)] = frequency;
        return map;
    }

    //User the host behaviour a vibration system drives, maps to vanilla VibrationSystem.User
    //The host decides what it can receive and what happens once a vibration arrives
    public interface User
    {
        //ListenerRadius how far this listener reaches, in blocks
        int ListenerRadius { get; }

        //PositionSource where this listener sits
        PositionSource PositionSource { get; }

        //CanReceiveVibration whether this specific vibration is acceptable from that position
        bool CanReceiveVibration(ServerLevel level, BlockPos pos, GameEvent gameEvent, GameEvent.Context context);

        //OnReceiveVibration the vibration arrived; entity is the direct source and projectileOwner its shooter
        void OnReceiveVibration(ServerLevel level, BlockPos pos, GameEvent gameEvent,
            NacreCraft.Registry.Entity? entity, NacreCraft.Registry.Entity? projectileOwner, float distance);

        //CanTriggerAvoidVibration whether sneaking past should fire the avoid advancement, sculk sensors set it
        bool CanTriggerAvoidVibration => false;

        //RequiresAdjacentChunksToBeTicking whether the eight neighbours must be loaded, the warden requires it
        bool RequiresAdjacentChunksToBeTicking => false;

        //CalculateTravelTimeInTicks how long the vibration takes to arrive, one tick per block by default
        int CalculateTravelTimeInTicks(float distanceToDestination) => Mth.Floor(distanceToDestination);

        //GetListenableEvents the tag set this user listens to, maps to vanilla getListenableEvents
        TagKey<GameEvent> ListenableEvents => GameEventTags.Vibrations;

        //IsValidVibration whether the event is worth listening to at all, maps to vanilla isValidVibration
        //Filtered by the listenable tag set, then by the source being a spectator, stepping carefully or damping,
        //and finally by the affected block damping vibrations
        bool IsValidVibration(GameEvent gameEvent, GameEvent.Context context)
        {
            if (!IsInTag(gameEvent, ListenableEvents)) return false;
            if (context.SourceEntity is { } sourceEntity)
            {
                if (sourceEntity.IsSpectator()) return false;
                //Stepping carefully walks past a sensor, but only for the events marked as ignorable while sneaking
                if (sourceEntity.IsCrouching && IsInTag(gameEvent, GameEventTags.IgnoreVibrationsSneaking)) return false;
                if (sourceEntity.DampensVibrations()) return false;
            }
            return context.AffectedState is not { } affectedState || !affectedState.Is(BlockTags.DampensVibrations);
        }

        //IsInTag whether the event belongs to a game event tag, maps to vanilla Holder.is(TagKey)
        internal static bool IsInTag(GameEvent gameEvent, TagKey<GameEvent> tag)
            => BuiltInRegistries.GAME_EVENT.Get(tag)
                ?.Contains(BuiltInRegistries.GAME_EVENT.WrapAsHolder(gameEvent)) == true;

        //OnDataChanged the host persists the vibration state, called after every change
        void OnDataChanged()
        {
        }
    }

    //Data the per-listener vibration state, maps to vanilla VibrationSystem.Data
    //Holds the vibration in flight, the countdown and the per-tick selector
    public sealed class Data
    {
        //NbtTagKey the save key, maps to vanilla NBT_TAG_KEY
        public const string NbtTagKey = "listener";

        public Data()
            : this(null, new VibrationSelector(), 0, false)
        {
        }

        public Data(VibrationInfo? currentVibration, VibrationSelector selectionStrategy, int travelTimeInTicks,
            bool reloadVibrationParticle)
        {
            CurrentVibration = currentVibration;
            SelectionStrategy = selectionStrategy;
            TravelTimeInTicks = travelTimeInTicks;
            ReloadVibrationParticle = reloadVibrationParticle;
        }

        //CurrentVibration the vibration travelling to the listener, null when idle
        public VibrationInfo? CurrentVibration { get; set; }

        //TravelTimeInTicks ticks left before it arrives
        public int TravelTimeInTicks { get; set; }

        //SelectionStrategy the per-tick candidate selection
        public VibrationSelector SelectionStrategy { get; }

        //ReloadVibrationParticle whether the flight particle needs to be resent after a reload
        public bool ReloadVibrationParticle { get; set; }

        //DecrementTravelTime advances the countdown, maps to vanilla decrementTravelTime
        public void DecrementTravelTime() => TravelTimeInTicks = Math.Max(0, TravelTimeInTicks - 1);
    }

    //Ticker advances one vibration system per tick, maps to vanilla VibrationSystem.Ticker
    public static class Ticker
    {
        //Tick advances the system: pick a candidate, count down, deliver on arrival
        public static void Tick(ServerLevel level, Data data, User user)
        {
            if (data.CurrentVibration is null) TrySelectAndScheduleVibration(level, data, user);
            if (data.CurrentVibration is null) return;

            var changed = data.TravelTimeInTicks > 0;
            TryReloadVibrationParticle(level, data, user);
            data.DecrementTravelTime();
            if (data.TravelTimeInTicks <= 0) changed = ReceiveVibration(level, data, user, data.CurrentVibration);
            if (changed) user.OnDataChanged();
        }

        //TrySelectAndScheduleVibration takes the tick's winner and starts its flight, maps to vanilla method of the same name
        private static void TrySelectAndScheduleVibration(ServerLevel level, Data data, User user)
        {
            if (data.SelectionStrategy.ChosenCandidate(level.GameTime) is not { } vibration) return;
            data.CurrentVibration = vibration;
            var origin = vibration.Pos;
            data.TravelTimeInTicks = user.CalculateTravelTimeInTicks(vibration.Distance);
            //The flight particle flies from the source to the listener over the travel time, maps to vanilla sendParticles
            level.SendParticles(
                new VibrationParticleOption(ParticleTypes.Vibration, user.PositionSource, data.TravelTimeInTicks),
                origin, 1);
            user.OnDataChanged();
            data.SelectionStrategy.StartOver();
        }

        //TryReloadVibrationParticle resends the flight particle partway along, maps to vanilla method of the same name
        //Vanilla needs this because the particle only lasts as long as the client was told; it is only reloaded when the
        //state was loaded from disk mid flight, which is why the flag exists at all
        private static void TryReloadVibrationParticle(ServerLevel level, Data data, User user)
        {
            if (!data.ReloadVibrationParticle) return;
            if (data.CurrentVibration is not { } vibration)
            {
                data.ReloadVibrationParticle = false;
                return;
            }
            var origin = vibration.Pos;
            var positionSource = user.PositionSource;
            var destination = positionSource.GetPosition(level) ?? origin;
            var initialTravelTime = user.CalculateTravelTimeInTicks(vibration.Distance);
            var alpha = initialTravelTime == 0 ? 1.0 : 1.0 - (double)data.TravelTimeInTicks / initialTravelTime;
            level.SendParticles(
                new VibrationParticleOption(ParticleTypes.Vibration, positionSource, data.TravelTimeInTicks),
                new Vec3(Mth.Lerp(alpha, origin.X, destination.X), Mth.Lerp(alpha, origin.Y, destination.Y),
                    Mth.Lerp(alpha, origin.Z, destination.Z)),
                1);
            data.ReloadVibrationParticle = false;
        }

        //ReceiveVibration delivers the arrived vibration to the user, maps to vanilla receiveVibration
        private static bool ReceiveVibration(ServerLevel level, Data data, User user, VibrationInfo currentVibration)
        {
            var origin = BlockPosOf(currentVibration.Pos);
            var destination = user.PositionSource.GetPosition(level) is { } position ? BlockPosOf(position) : origin;
            if (user.RequiresAdjacentChunksToBeTicking && !AreAdjacentChunksTicking(level, destination)) return false;
            user.OnReceiveVibration(level, origin, currentVibration.GameEvent,
                currentVibration.GetEntity(level), currentVibration.GetProjectileOwnerEntity(level),
                Listener.DistanceBetweenInBlocks(origin, destination));
            data.CurrentVibration = null;
            return true;
        }

        //AreAdjacentChunksTicking whether the eight neighbours of the listener are loaded, maps to vanilla method of the same name
        private static bool AreAdjacentChunksTicking(ServerLevel level, BlockPos listenerPos)
        {
            var chunkX = listenerPos.X >> 4;
            var chunkZ = listenerPos.Z >> 4;
            for (var x = chunkX - 1; x <= chunkX + 1; x++)
            {
                for (var z = chunkZ - 1; z <= chunkZ + 1; z++)
                {
                    if (level.GetChunk(new ChunkPos(x, z)) is null) return false;
                }
            }
            return true;
        }

        //BlockPosOf rounds a world position down to the block it sits in
        public static BlockPos BlockPosOf(Vec3 position)
            => new(Mth.Floor(position.X), Mth.Floor(position.Y), Mth.Floor(position.Z));
    }

    //Listener the game event listener half of the system, maps to vanilla VibrationSystem.Listener
    //Events that reach it become candidates; the ticker later picks and delivers one
    public sealed class Listener(VibrationSystem system) : GameEventListener
    {
        public PositionSource ListenerSource => system.VibrationUser.PositionSource;

        public int ListenerRadius => system.VibrationUser.ListenerRadius;

        public bool HandleGameEvent(ServerLevel level, GameEvent gameEvent, GameEvent.Context context, Vec3 position)
        {
            var data = system.VibrationData;
            var user = system.VibrationUser;
            if (data.CurrentVibration is not null) return false;
            if (!user.IsValidVibration(gameEvent, context)) return false;
            if (user.PositionSource.GetPosition(level) is not { } destination) return false;
            if (!user.CanReceiveVibration(level, Ticker.BlockPosOf(position), gameEvent, context)) return false;
            if (IsOccluded(level, position, destination)) return false;
            ScheduleVibration(level, data, gameEvent, context, position, destination);
            return true;
        }

        //ForceScheduleVibration schedules a vibration regardless of the usual filtering, maps to vanilla method of the same name
        public void ForceScheduleVibration(ServerLevel level, GameEvent gameEvent, GameEvent.Context context, Vec3 origin)
        {
            if (system.VibrationUser.PositionSource.GetPosition(level) is { } destination)
                ScheduleVibration(level, system.VibrationData, gameEvent, context, origin, destination);
        }

        private static void ScheduleVibration(ServerLevel level, Data data, GameEvent gameEvent,
            GameEvent.Context context, Vec3 origin, Vec3 destination)
            => data.SelectionStrategy.AddCandidate(
                new VibrationInfo(gameEvent, (float)origin.DistanceTo(destination), origin, context.SourceEntity),
                level.GameTime);

        //DistanceBetweenInBlocks the straight line distance between two block positions, maps to vanilla method of the same name
        public static float DistanceBetweenInBlocks(BlockPos origin, BlockPos destination)
        {
            var dx = origin.X - destination.X;
            var dy = origin.Y - destination.Y;
            var dz = origin.Z - destination.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        //IsOccluded whether a signal blocking block sits between the source and the listener, maps to vanilla isOccluded
        //Every direction is tried from a hair outside the origin block; the vibration is blocked only when all of them are cut off
        private static bool IsOccluded(ServerLevel level, Vec3 origin, Vec3 destination)
        {
            if (level is not PersistentServerLevel persistent) return false;
            var from = new Vec3(Mth.Floor(origin.X) + 0.5, Mth.Floor(origin.Y) + 0.5, Mth.Floor(origin.Z) + 0.5);
            var to = new Vec3(Mth.Floor(destination.X) + 0.5, Mth.Floor(destination.Y) + 0.5,
                Mth.Floor(destination.Z) + 0.5);
            foreach (var direction in Direction.Values)
            {
                if (ServerBlockRaycast.ClipBlockPos(persistent, Nudge(from, direction), to,
                        state => state.Is(BlockTags.OccludesVibrationSignals)) is null)
                    return false;
            }
            return true;
        }

        //Nudge shifts a point a hair along one direction, maps to vanilla Vec3.relative
        //The nudge keeps a ray that starts inside a block from immediately reporting that block as the blocker
        private static Vec3 Nudge(Vec3 from, Direction direction)
        {
            const double epsilon = 1.0E-5;
            var offset = new Vec3i(0, 0, 0).Relative(direction, 1);
            return from.Add(offset.X * epsilon, offset.Y * epsilon, offset.Z * epsilon);
        }
    }
}
