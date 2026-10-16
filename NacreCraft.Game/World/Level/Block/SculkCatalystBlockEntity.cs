using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level.GameEvents;
using NacreCraft.Game.World.Particle;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//SculkCatalystBlockEntity the sculk catalyst's brain, maps to vanilla SculkCatalystBlockEntity
//It listens for deaths within eight blocks and turns the experience the victim would have dropped into sculk charge,
//which the spreader then walks outward across the veins around it
public sealed class SculkCatalystBlockEntity : BlockEntity
{
    public SculkCatalystBlockEntity(BlockPos pos) : base(BlockEntityTypes.SCULK_CATALYST, pos)
        => Listener = new CatalystListener(new BlockPositionSource(pos));

    //Listener the game event listener the level registers, maps to vanilla getListener
    public CatalystListener Listener { get; }

    //Tick advances the charge spreader, maps to vanilla serverTick
    public override void Tick()
    {
        if (Level is not { } level) return;
        //The spreader is shared with world generation and talks to the level through the block access
        Listener.Spreader.UpdateCursors(new ServerLevelSculkAccess(level), Pos, level.Random, true);
    }

    //CatalystListener turns nearby deaths into charge, maps to vanilla SculkCatalystBlockEntity.CatalystListener
    public sealed class CatalystListener : GameEventListener
    {
        //PulseTicks how long the bloom shows, maps to vanilla PULSE_TICKS
        public const int PulseTicks = 8;

        //ExperiencePerDeath the charge one death feeds in
        //Vanilla uses the victim's own experience reward; this port has no experience system, so a flat reward stands in
        private const int ExperiencePerDeath = 5;

        private readonly PositionSource _positionSource;

        public CatalystListener(PositionSource positionSource) => _positionSource = positionSource;

        public SculkSpreader Spreader { get; } = SculkSpreader.CreateLevelSpreader();

        public PositionSource ListenerSource => _positionSource;

        public int ListenerRadius => 8;

        //Mode deaths at different distances compete, so the nearest one wins the tick, maps to vanilla BY_DISTANCE
        public DeliveryMode Mode => DeliveryMode.ByDistance;

        //HandleGameEvent a death nearby feeds the spreader, maps to vanilla handleGameEvent
        //Vanilla also suppresses the victim's own experience drop and awards an advancement for it; neither has a
        //counterpart here, the first for lack of an experience system and the second for lack of advancements
        public bool HandleGameEvent(ServerLevel level, GameEvent gameEvent, GameEvent.Context context, Vec3 position)
        {
            if (!ReferenceEquals(gameEvent, GameEvent.EntityDie)) return false;
            if (context.SourceEntity is not LivingEntity) return false;
            //The charge starts half a block up so it lands on the cell the victim died in
            Spreader.AddCursors(FloorOf(position, 0.5), ExperiencePerDeath);
            if (_positionSource.GetPosition(level) is { } bloomPosition) Bloom(level, FloorOf(bloomPosition, 0.0));
            return true;
        }

        //Bloom flashes the catalyst and spits sculk souls, maps to vanilla bloom
        private static void Bloom(ServerLevel level, BlockPos pos)
        {
            if (level.GetBlockState(pos) is not { } state) return;
            level.SetBlock(pos, state.SetValue(BlockStateProperties.Bloom, true));
            level.ScheduleTick(pos, state.Owner, PulseTicks);
            level.SendParticles(ParticleTypes.SculkSoul, new Vec3(pos.X + 0.5, pos.Y + 1.15, pos.Z + 0.5), 2);
        }

        //FloorOf the block position a world position falls in, with an optional lift first, maps to vanilla BlockPos.containing
        private static BlockPos FloorOf(Vec3 position, double lift)
            => new((int)Math.Floor(position.X), (int)Math.Floor(position.Y + lift), (int)Math.Floor(position.Z));
    }
}
