using NacreCraft.Game.World.Level.GameEvents;
using NacreCraft.Game.World.Level.GameEvents.Vibrations;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//SculkSensorBlockEntity the sculk sensor's brain, maps to vanilla net.minecraft.world.level.block.entity.SculkSensorBlockEntity
//It owns a vibration system: the listener collects vibrations within eight blocks, the ticker flies them in, and the
//winner is handed back here, which activates the block and sets the redstone strength from the distance travelled
public sealed class SculkSensorBlockEntity : BlockEntity, VibrationSystem
{
    //ListenerRange how far the sensor reaches, maps to vanilla LISTENER_RANGE
    public const int ListenerRange = 8;

    private readonly VibrationSystem.Listener _listener;
    private readonly VibrationSystem.User _user;

    public SculkSensorBlockEntity(BlockPos pos) : base(BlockEntityTypes.SCULK_SENSOR, pos)
    {
        _user = new SensorVibrationUser(this, pos);
        _listener = new VibrationSystem.Listener(this);
    }

    //LastVibrationFrequency the frequency of the last accepted vibration, read as the comparator output
    public int LastVibrationFrequency { get; set; }

    public VibrationSystem.Data VibrationData { get; } = new();

    public VibrationSystem.User VibrationUser => _user;

    //Listener the game event listener the level registers, maps to vanilla getListener
    public VibrationSystem.Listener Listener => _listener;

    //BlockState the state of the block this entity sits on
    //The block entity base carries no state here, so it is read back from the level
    public BlockState? BlockState => Level?.GetBlockState(Pos);

    //Tick advances the vibration system, maps to the vanilla block entity ticker
    public override void Tick()
    {
        if (Level is { } level) VibrationSystem.Ticker.Tick(level, VibrationData, _user);
    }

    //SaveAdditional persists the last frequency, maps to vanilla saveAdditional
    //The vibration data codec is not wired up yet, so an in-flight vibration is lost across a save, see VibrationInfo
    public override void SaveAdditional(CompoundTag tag)
        => tag.PutInt("last_vibration_frequency", LastVibrationFrequency);

    //LoadAdditional reads it back, maps to vanilla loadAdditional
    public override void LoadAdditional(CompoundTag tag)
        => LastVibrationFrequency = tag.GetIntOr("last_vibration_frequency", 0);

    //SensorVibrationUser the sensor's vibration host behaviour, maps to vanilla SculkSensorBlockEntity.VibrationUser
    private sealed class SensorVibrationUser(SculkSensorBlockEntity sensor, BlockPos pos) : VibrationSystem.User
    {
        private readonly BlockPositionSource _positionSource = new(pos);

        public int ListenerRadius => ListenerRange;

        public PositionSource PositionSource => _positionSource;

        //CanTriggerAvoidVibration walking past carefully is exactly what a sensor is about, maps to vanilla
        public bool CanTriggerAvoidVibration => true;

        //RequiresAdjacentChunksToBeTicking the eight neighbours must be loaded for the sensor to hear, maps to vanilla
        public bool RequiresAdjacentChunksToBeTicking => true;

        //CanReceiveVibration a sensor ignores its own placement and removal, silent events and one that is not idle
        //GameEvent is a value record, so the two events are compared by reference: a value compare would treat every
        //event that shares a notification radius as the same one
        public bool CanReceiveVibration(ServerLevel level, BlockPos sourcePos, GameEvent gameEvent,
            GameEvent.Context context)
        {
            if (sourcePos == pos
                && (ReferenceEquals(gameEvent, GameEvent.BlockDestroy) || ReferenceEquals(gameEvent, GameEvent.BlockPlace)))
                return false;
            if (VibrationSystem.GetGameEventFrequency(gameEvent) == 0) return false;
            return sensor.BlockState is { } state && Blocks.SculkSensorBlock.CanActivate(state);
        }

        //OnReceiveVibration the vibration arrived: remember its frequency and activate the block, maps to vanilla
        public void OnReceiveVibration(ServerLevel level, BlockPos sourcePos, GameEvent gameEvent,
            NacreCraft.Registry.Entity? entity, NacreCraft.Registry.Entity? projectileOwner, float receivingDistance)
        {
            if (sensor.BlockState is not { } state || !Blocks.SculkSensorBlock.CanActivate(state)) return;
            var frequency = VibrationSystem.GetGameEventFrequency(gameEvent);
            sensor.LastVibrationFrequency = frequency;
            var power = VibrationSystem.GetRedstoneStrengthForDistance(receivingDistance, ListenerRadius);
            if (state.Owner is Blocks.SculkSensorBlock block) block.Activate(entity, level, pos, state, power, frequency);
        }

        //OnDataChanged tells the level the saved data changed, maps to vanilla setChanged
        public void OnDataChanged() => sensor.Level?.BlockEntityChanged(sensor.Pos);
    }
}
