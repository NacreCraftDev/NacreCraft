using NacreCraft.Game.World.Level.GameEvents;
using NacreCraft.Game.World.Level.GameEvents.Vibrations;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//SculkShriekerBlockEntity the sculk shrieker's brain, maps to vanilla SculkShriekerBlockEntity
//It listens for the events in SHRIEKER_CAN_LISTEN, and screams at the player that caused one
public sealed class SculkShriekerBlockEntity : BlockEntity, VibrationSystem
{
    //LevelEventShriek the particle event a scream broadcasts, maps to vanilla 3007
    private const int LevelEventShriek = 3007;

    private readonly VibrationSystem.User _user;
    private readonly VibrationSystem.Listener _listener;

    public SculkShriekerBlockEntity(BlockPos pos) : base(BlockEntityTypes.SCULK_SHRIEKER, pos)
    {
        _user = new ShriekerVibrationUser(this, pos);
        _listener = new VibrationSystem.Listener(this);
    }

    public VibrationSystem.Data VibrationData { get; } = new();

    public VibrationSystem.User VibrationUser => _user;

    public VibrationSystem.Listener Listener => _listener;

    public BlockState? BlockState => Level?.GetBlockState(Pos);

    //Tick advances the vibration system, maps to the vanilla block entity ticker
    public override void Tick()
    {
        if (Level is { } level) VibrationSystem.Ticker.Tick(level, VibrationData, _user);
    }

    //TryShriek scream at the source of a vibration, maps to vanilla tryShriek
    //Vanilla first warns the player and only screams once the warnings reach the fourth level; the warden spawn tracker
    //that counts them is not ported, so this screams straight away
    public void TryShriek(ServerLevel level, NacreCraft.Registry.Entity? sourceEntity)
    {
        if (BlockState is not { } state || state.GetValue(BlockStateProperties.Shrieking)) return;
        Shriek(level, state, sourceEntity);
    }

    private void Shriek(ServerLevel level, BlockState state, NacreCraft.Registry.Entity? sourceEntity)
    {
        level.SetBlock(Pos, state.SetValue(BlockStateProperties.Shrieking, true));
        level.ScheduleTick(Pos, state.Owner, Blocks.SculkShriekerBlock.ShriekingTicks);
        level.LevelEvent(LevelEventShriek, Pos, 0);
        level.GameEvent(sourceEntity, NacreCraft.Registry.GameEvent.Shriek, new Vec3(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5));
    }

    //TryRespond the scream finished: this is where a warden gets summoned, maps to vanilla tryRespond
    //The warden entity and the darkness effect around the shrieker do not exist yet, so the call is kept but does nothing
    public void TryRespond(ServerLevel level)
    {
    }

    //TryGetPlayer resolves the player behind an entity, maps to vanilla tryGetPlayer
    //It accepts the player itself, whoever rides it, the shooter of a projectile, or the thrower of an item
    //This port has two separate entity hierarchies (the network player and the registry entity), and the registry side
    //carries no link back, so the entity is passed through unchanged
    public static NacreCraft.Registry.Entity? TryGetPlayer(NacreCraft.Registry.Entity? sourceEntity) => sourceEntity;

    //ShriekerVibrationUser the shrieker's vibration host behaviour, maps to vanilla SculkShriekerBlockEntity.VibrationUser
    private sealed class ShriekerVibrationUser(SculkShriekerBlockEntity shrieker, BlockPos pos) : VibrationSystem.User
    {
        private readonly BlockPositionSource _positionSource = new(pos);

        public int ListenerRadius => 8;

        public PositionSource PositionSource => _positionSource;

        //The shrieker hears only the events tagged for it, maps to vanilla getListenableEvents
        public TagKey<GameEvent> ListenableEvents => GameEventTags.ShriekerCanListen;

        //CanReceiveVibration one that is already screaming cannot start again, and only a player caused event counts
        public bool CanReceiveVibration(ServerLevel level, BlockPos sourcePos, GameEvent gameEvent,
            GameEvent.Context context)
            => shrieker.BlockState is { } state && !state.GetValue(BlockStateProperties.Shrieking)
                && TryGetPlayer(context.SourceEntity) is not null;

        //OnReceiveVibration scream at whoever caused it, preferring the projectile owner, maps to vanilla
        public void OnReceiveVibration(ServerLevel level, BlockPos sourcePos, GameEvent gameEvent,
            NacreCraft.Registry.Entity? entity, NacreCraft.Registry.Entity? projectileOwner, float receivingDistance)
            => shrieker.TryShriek(level, projectileOwner ?? entity);

        public void OnDataChanged() => shrieker.Level?.BlockEntityChanged(shrieker.Pos);

        public bool RequiresAdjacentChunksToBeTicking => true;
    }
}
