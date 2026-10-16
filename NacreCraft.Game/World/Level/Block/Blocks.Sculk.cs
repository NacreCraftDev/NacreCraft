using NacreCraft.Game.World.Level.GameEvents.Vibrations;
using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Primitives.Phys;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.Block;

//Sculk family blocks, maps to the sculk blocks of vanilla net.minecraft.world.level.block
//The sensor goes first: it is the host of the vibration system and its only extra dependency is the block entity layer,
//whereas the spreading side (sculk, sculk vein, catalyst) needs the multiface block hierarchy and the charge spreader
public static partial class Blocks
{
    public static readonly SculkSensorBlock SCULK_SENSOR = new();

    public static readonly SculkVeinBlock SCULK_VEIN = new();

    public static readonly SculkBlock SCULK = new();

    public static readonly SculkCatalystBlock SCULK_CATALYST = new();

    public static readonly SculkShriekerBlock SCULK_SHRIEKER = new();

    //RegisterSculk registers the sculk family into the real block table, see Blocks.Bootstrap
    private static void RegisterSculk(Dictionary<string, BlockBehaviour> real)
    {
        real[SCULK_SENSOR.Id.Path] = SCULK_SENSOR;
        real[SCULK_VEIN.Id.Path] = SCULK_VEIN;
        real[SCULK.Id.Path] = SCULK;
        real[SCULK_CATALYST.Id.Path] = SCULK_CATALYST;
        real[SCULK_SHRIEKER.Id.Path] = SCULK_SHRIEKER;
    }

    //SculkState the plain sculk block's default state
    //That block is still a placeholder from the table, so its state is looked up rather than declared here
    internal static BlockState SculkState => LookupState("sculk");

    internal static BlockState SculkSensorState => LookupState("sculk_sensor");

    internal static BlockState SculkShriekerState => LookupState("sculk_shrieker");

    internal static BlockState WaterState => LookupState("water");

    private static BlockState LookupState(string path)
        => NacreCraft.Registry.BuiltInRegistries.BLOCK.GetValue(Identifier.WithDefaultNamespace(path))?.DefaultBlockState
            ?? AIR.DefaultBlockState;

    //SculkSensorBlock sculk sensor, maps to vanilla net.minecraft.world.level.block.SculkSensorBlock
    //Listens for vibrations within eight blocks, stays active for thirty ticks and emits redstone strength by distance
    //The sound layer and the waterlogged fluid tick are absent here, so those branches are dropped
    public sealed class SculkSensorBlock : BlockBehaviour
    {
        //ActiveTicks how long it stays active, maps to vanilla ACTIVE_TICKS
        public const int ActiveTicks = 30;

        //CooldownTicks how long it stays on cooldown afterwards, maps to vanilla COOLDOWN_TICKS
        public const int CooldownTicks = 10;

        public override Identifier Id => Identifier.WithDefaultNamespace("sculk_sensor");

        //Properties states follow power|sculk_sensor_phase|waterlogged in blocks.txt
        public override IDictionary<string, PropertyBase> Properties => new Dictionary<string, PropertyBase>
        {
            ["power"] = BlockStateProperties.Power,
            ["sculk_sensor_phase"] = BlockStateProperties.SculkSensorPhaseProperty,
            ["waterlogged"] = BlockStateProperties.Waterlogged,
        };

        //IsSignalSource it drives redstone, maps to vanilla isSignalSource
        public override bool IsSignalSource => true;

        //SensorShape the sensor is a slab-high box, maps to vanilla SHAPE
        private static readonly VoxelShape SensorShape = NacreCraft.Registry.Block.Column(16.0, 0.0, 8.0);

        //GetShape a low box rather than a full cube, maps to vanilla getShape
        public override VoxelShape GetShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context)
            => SensorShape;

        //CreateBlockEntity the sensor keeps its vibration state in a block entity, maps to vanilla newBlockEntity
        public override BlockEntity? CreateBlockEntity(BlockPos pos, BlockState state) => new SculkSensorBlockEntity(pos);

        //Tick the phase countdown: active falls to cooldown, cooldown falls to inactive, maps to vanilla tick
        public override void Tick(ServerLevel level, BlockPos pos, BlockState state, RandomSource random)
        {
            var phase = GetPhase(state);
            if (phase == NacreCraft.Registry.Enums.SculkSensorPhase.active)
            {
                Deactivate(level, pos, state);
                return;
            }
            if (phase != NacreCraft.Registry.Enums.SculkSensorPhase.cooldown) return;
            level.SetBlock(pos, SetPhase(state, NacreCraft.Registry.Enums.SculkSensorPhase.inactive));
        }

        //AffectNeighborsAfterRemoval an active sensor stops powering its neighbours once removed, maps to vanilla
        public override void AffectNeighborsAfterRemoval(ServerLevel level, BlockPos pos, BlockState state,
            bool movedByPiston)
        {
            if (GetPhase(state) == NacreCraft.Registry.Enums.SculkSensorPhase.active)
                UpdateNeighbours(level, pos, state);
        }

        //OwnSignal the sensor's own strength is its power state, maps to vanilla ownSignal
        public override int OwnSignal(ServerLevel level, BlockPos pos, BlockState state)
            => state.GetValue(BlockStateProperties.Power);

        //GetDirectSignal only the top face passes the signal on directly, maps to vanilla getDirectSignal
        public override int GetDirectSignal(ServerLevel level, BlockPos pos, BlockState state, Direction direction)
            => direction == Direction.Up ? GetSignal(level, pos, state, direction) : 0;

        //HasAnalogOutputSignal comparators read the last vibration frequency, maps to vanilla
        public override bool HasAnalogOutputSignal => true;

        //GetAnalogOutputSignal the comparator sees the last frequency while the sensor is active, maps to vanilla
        public override int GetAnalogOutputSignal(ServerLevel level, BlockPos pos, BlockState state, Direction direction)
            => GetPhase(state) == NacreCraft.Registry.Enums.SculkSensorPhase.active
                && level.GetBlockEntity<SculkSensorBlockEntity>(pos) is { } sensor
                    ? sensor.LastVibrationFrequency
                    : 0;

        //GetPhase the phase of a sensor state, maps to vanilla getPhase
        public static NacreCraft.Registry.Enums.SculkSensorPhase GetPhase(BlockState state)
            => state.GetValue(BlockStateProperties.SculkSensorPhaseProperty);

        //CanActivate whether the sensor is idle and can take a vibration, maps to vanilla canActivate
        public static bool CanActivate(BlockState state)
            => GetPhase(state) == NacreCraft.Registry.Enums.SculkSensorPhase.inactive;

        private static BlockState SetPhase(BlockState state, NacreCraft.Registry.Enums.SculkSensorPhase phase)
            => state.SetValue(BlockStateProperties.SculkSensorPhaseProperty, phase);

        //Deactivate drop to cooldown and clear the power, maps to vanilla deactivate
        public static void Deactivate(ServerLevel level, BlockPos pos, BlockState state)
        {
            level.SetBlock(pos, SetPhase(state, NacreCraft.Registry.Enums.SculkSensorPhase.cooldown)
                .SetValue(BlockStateProperties.Power, 0));
            level.ScheduleTick(pos, state.Owner, CooldownTicks);
            UpdateNeighbours(level, pos, state);
        }

        //UpdateNeighbours the sensor powers its own cell and the one below, maps to vanilla updateNeighbours
        private static void UpdateNeighbours(ServerLevel level, BlockPos pos, BlockState state)
        {
            var block = state.Owner;
            level.UpdateNeighborsAt(pos, block);
            level.UpdateNeighborsAt(pos.Offset(0, -1, 0), block);
        }

        //Activate a vibration landed: go active, power by distance and ping nearby resonators, maps to vanilla activate
        public void Activate(NacreCraft.Registry.Entity? source, ServerLevel level, BlockPos pos, BlockState state,
            int calculatedPower, int vibrationFrequency)
        {
            level.SetBlock(pos, SetPhase(state, NacreCraft.Registry.Enums.SculkSensorPhase.active)
                .SetValue(BlockStateProperties.Power, calculatedPower));
            level.ScheduleTick(pos, state.Owner, ActiveTicks);
            UpdateNeighbours(level, pos, state);
            TryResonateVibration(source, level, pos, vibrationFrequency);
            level.GameEvent(source, NacreCraft.Registry.GameEvent.SculkSensorTendrilsClicking,
                new Vec3(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5));
        }

        //TryResonateVibration tells the resonators around it, maps to vanilla tryResonateVibration
        //Vanilla also plays the amethyst resonate sound at a pitch derived from the frequency; there is no sound layer here
        public static void TryResonateVibration(NacreCraft.Registry.Entity? source, ServerLevel level, BlockPos pos,
            int vibrationFrequency)
        {
            foreach (var direction in Direction.Values)
            {
                var relativePos = pos.Offset(direction);
                if (level.GetBlockState(relativePos) is not { } neighbour) continue;
                if (!neighbour.Is(BlockTags.VibrationResonators)) continue;
                level.GameEvent(null, VibrationSystem.GetResonanceEventByFrequency(vibrationFrequency),
                    new Vec3(relativePos.X + 0.5, relativePos.Y + 0.5, relativePos.Z + 0.5),
                    NacreCraft.Registry.GameEvent.Context.Of(source, neighbour));
            }
        }
    }

    //SculkBlock plain sculk, maps to vanilla net.minecraft.world.level.block.SculkBlock
    //A charge sitting on sculk spends itself growing more sculk above, and sometimes a sensor or a shrieker with it
    public sealed class SculkBlock : BlockBehaviour, SculkBehaviour
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("sculk");

        //CanChangeBlockStateOnSpread sculk keeps standing on whatever it grew over, maps to vanilla
        public bool CanChangeBlockStateOnSpread => false;

        //AttemptUseCharge sculk spends charge on a growth above itself, maps to vanilla attemptUseCharge
        //Inside the no-growth radius it is not allowed to grow, so the charge only thins out there
        public int AttemptUseCharge(SculkSpreader.ChargeCursor cursor, ISculkBlockAccess level, BlockPos originPos,
            RandomSource random, SculkSpreader spreader, bool spreadVeins)
        {
            var charge = cursor.Charge;
            if (charge == 0 || random.NextInt(spreader.ChargeDecayRate) != 0) return charge;
            var chargePos = cursor.Pos;
            var closeToCatalyst = IsCloserThan(chargePos, originPos, spreader.NoGrowthRadius);
            if (closeToCatalyst || !CanPlaceGrowth(level, chargePos))
            {
                if (random.NextInt(spreader.AdditionalDecayRate) != 0) return charge;
                return charge - (closeToCatalyst ? 1 : GetDecayPenalty(spreader, chargePos, originPos, charge));
            }
            var growthCost = spreader.GrowthSpawnCost;
            if (random.NextInt(growthCost) < charge)
            {
                var growthPos = chargePos.Offset(0, 1, 0);
                var growthState = GetRandomGrowthState(level, growthPos, random, spreader.IsWorldGeneration);
                level.SetBlock(growthPos, growthState);
            }
            return Math.Max(0, charge - growthCost);
        }

        //GetDecayPenalty what the ring outside the no-growth radius costs, maps to vanilla getDecayPenalty
        //The further out from the catalyst, the more charge a step burns, capped at half the charge
        private static int GetDecayPenalty(SculkSpreader spreader, BlockPos pos, BlockPos originPos, int charge)
        {
            var noGrowthRadius = spreader.NoGrowthRadius;
            var outerDistanceSquared = (float)Math.Pow(Math.Sqrt(DistanceSquared(pos, originPos)) - noGrowthRadius, 2);
            var maxReachSquared = (float)Math.Pow(24 - noGrowthRadius, 2);
            var distanceFactor = Math.Min(1.0f, outerDistanceSquared / maxReachSquared);
            return Math.Max(1, (int)(charge * distanceFactor * 0.5f));
        }

        //GetRandomGrowthState one in SHRIEKER_PLACEMENT_RATE growths is a shrieker, the rest are sensors
        //maps to vanilla getRandomGrowthState; a cell holding water gets the growth waterlogged as well
        private static BlockState GetRandomGrowthState(ISculkBlockAccess level, BlockPos pos, RandomSource random,
            bool isWorldGen)
        {
            var state = random.NextInt(SculkSpreader.ShriekerPlacementRate) == 0
                ? Blocks.SculkShriekerState.SetValue(BlockStateProperties.CanSummon, isWorldGen)
                : Blocks.SculkSensorState;
            if (level.GetBlockState(pos) is { } at && !at.FluidState.IsEmpty)
                state = state.SetValue(BlockStateProperties.Waterlogged, true);
            return state;
        }

        //CanPlaceGrowth whether the cell above is free and there are not already too many growths around
        //maps to vanilla canPlaceGrowth: at most two growths may sit in the 9x3x9 box around the cell
        private static bool CanPlaceGrowth(ISculkBlockAccess level, BlockPos pos)
        {
            var abovePos = pos.Offset(0, 1, 0);
            if (level.GetBlockState(abovePos) is not { } above) return false;
            if (!above.Owner.IsAir && !above.Is(Blocks.WaterState.Owner)) return false;

            var growthCount = 0;
            for (var dx = -4; dx <= 4; dx++)
            {
                for (var dy = 0; dy <= 2; dy++)
                {
                    for (var dz = -4; dz <= 4; dz++)
                    {
                        if (level.GetBlockState(pos.Offset(dx, dy, dz)) is not { } state) continue;
                        if (state.Is(Blocks.SculkSensorState.Owner) || state.Is(Blocks.SculkShriekerState.Owner))
                            growthCount++;
                        if (growthCount > 2) return false;
                    }
                }
            }
            return true;
        }

        private static bool IsCloserThan(BlockPos pos, BlockPos origin, double distance)
            => Math.Sqrt(DistanceSquared(pos, origin)) < distance;

        private static double DistanceSquared(BlockPos first, BlockPos second)
        {
            var dx = first.X - second.X;
            var dy = first.Y - second.Y;
            var dz = first.Z - second.Z;
            return dx * dx + dy * dy + dz * dz;
        }
    }

    //SculkCatalystBlock sculk catalyst, maps to vanilla net.minecraft.world.level.block.SculkCatalystBlock
    //It listens for deaths nearby and turns the experience the victim would have dropped into sculk charge
    public sealed class SculkCatalystBlock : BlockBehaviour
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("sculk_catalyst");

        public override IDictionary<string, PropertyBase> Properties => new Dictionary<string, PropertyBase>
        {
            ["bloom"] = BlockStateProperties.Bloom,
        };

        //CreateBlockEntity the catalyst carries a charge spreader, maps to vanilla newBlockEntity
        public override BlockEntity? CreateBlockEntity(BlockPos pos, BlockState state)
            => new SculkCatalystBlockEntity(pos);

        //Tick clears the bloom pulse, maps to vanilla tick
        public override void Tick(ServerLevel level, BlockPos pos, BlockState state, RandomSource random)
        {
            if (state.GetValue(BlockStateProperties.Bloom))
                level.SetBlock(pos, state.SetValue(BlockStateProperties.Bloom, false));
        }
    }

    //SculkShriekerBlock sculk shrieker, maps to vanilla net.minecraft.world.level.block.SculkShriekerBlock
    //It hears vibrations and screams, which is the path to summoning a warden; the scream runs for SHRIEKING_TICKS
    //The step-on trigger is not wired up: this port's entity-inside hook carries no entity, so only the vibration path fires
    public sealed class SculkShriekerBlock : BlockBehaviour
    {
        //ShriekingTicks how long the scream holds, maps to vanilla SHRIEKING_TICKS
        public const int ShriekingTicks = 90;

        public override Identifier Id => Identifier.WithDefaultNamespace("sculk_shrieker");

        public override IDictionary<string, PropertyBase> Properties => new Dictionary<string, PropertyBase>
        {
            ["shrieking"] = BlockStateProperties.Shrieking,
            ["can_summon"] = BlockStateProperties.CanSummon,
            ["waterlogged"] = BlockStateProperties.Waterlogged,
        };

        //CreateBlockEntity the shrieker carries a vibration system, maps to vanilla newBlockEntity
        public override BlockEntity? CreateBlockEntity(BlockPos pos, BlockState state)
            => new SculkShriekerBlockEntity(pos);

        //Tick ends the scream and lets the block answer, maps to vanilla tick
        public override void Tick(ServerLevel level, BlockPos pos, BlockState state, RandomSource random)
        {
            if (!state.GetValue(BlockStateProperties.Shrieking)) return;
            level.SetBlock(pos, state.SetValue(BlockStateProperties.Shrieking, false));
            if (level.GetBlockEntity<SculkShriekerBlockEntity>(pos) is { } shrieker) shrieker.TryRespond(level);
        }
    }
}
