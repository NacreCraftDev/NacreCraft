using NacreCraft.Codec;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Util;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen.Features.Impl.Misc;

//SculkPatchConfiguration sculk patch configuration, maps to vanilla SculkPatchConfiguration
public sealed class SculkPatchConfiguration : FeatureConfiguration
{
    public static readonly Codec<SculkPatchConfiguration> Codec =
        RecordCodecBuilder.Of7<SculkPatchConfiguration, int, int, int, int, int, IntProvider, float>(
            Codecs.Int.FieldOf("charge_count").ForGetter<SculkPatchConfiguration, int>(c => c.ChargeCount),
            Codecs.Int.FieldOf("amount_per_charge").ForGetter<SculkPatchConfiguration, int>(c => c.AmountPerCharge),
            Codecs.Int.FieldOf("spread_attempts").ForGetter<SculkPatchConfiguration, int>(c => c.SpreadAttempts),
            Codecs.Int.FieldOf("growth_rounds").ForGetter<SculkPatchConfiguration, int>(c => c.GrowthRounds),
            Codecs.Int.FieldOf("spread_rounds").ForGetter<SculkPatchConfiguration, int>(c => c.SpreadRounds),
            IntProviders.Codec.FieldOf("extra_rare_growths")
                .ForGetter<SculkPatchConfiguration, IntProvider>(c => c.ExtraRareGrowths),
            Codecs.Float.FieldOf("catalyst_chance")
                .ForGetter<SculkPatchConfiguration, float>(c => c.CatalystChance),
            (chargeCount, amountPerCharge, spreadAttempts, growthRounds, spreadRounds, extraRareGrowths,
                catalystChance) => new SculkPatchConfiguration(chargeCount, amountPerCharge, spreadAttempts,
                growthRounds, spreadRounds, extraRareGrowths, catalystChance));

    public int ChargeCount { get; }
    public int AmountPerCharge { get; }
    public int SpreadAttempts { get; }
    public int GrowthRounds { get; }
    public int SpreadRounds { get; }
    public IntProvider ExtraRareGrowths { get; }
    public float CatalystChance { get; }

    public SculkPatchConfiguration(int chargeCount, int amountPerCharge, int spreadAttempts, int growthRounds,
        int spreadRounds, IntProvider extraRareGrowths, float catalystChance)
    {
        ChargeCount = chargeCount;
        AmountPerCharge = amountPerCharge;
        SpreadAttempts = spreadAttempts;
        GrowthRounds = growthRounds;
        SpreadRounds = spreadRounds;
        ExtraRareGrowths = extraRareGrowths;
        CatalystChance = catalystChance;
    }
}

//SculkPatchFeature sculk patch feature, maps to vanilla SculkPatchFeature
//It scatters charge-carrying cursors from the origin and lets them corrode whatever is around into sculk
//The spreader, the behaviour and the multiface logic are the shared ones in the block namespace: world generation
//reaches them through the block access, so the same code runs here and on a live server
//Spread rounds and attempt counts set how much random is consumed; a mismatch yields different patches for one seed
public sealed class SculkPatchFeature : Feature<SculkPatchConfiguration>
{
    private const string FeatureId = "sculk_patch";

    public static readonly SculkPatchFeature Instance = Register(
        Identifier.WithDefaultNamespace(FeatureId), new SculkPatchFeature());

    private SculkPatchFeature()
        : base(Identifier.WithDefaultNamespace(FeatureId), SculkPatchConfiguration.Codec)
    {
    }

    protected override bool Place(SculkPatchConfiguration config, FeaturePlaceContext context)
    {
        var level = context.Level;
        var origin = context.Origin;
        if (!CanSpreadFrom(level, origin)) return false;
        var random = context.Random;
        var access = new WorldGenRegionSculkAccess(level);
        var spreader = SculkSpreader.CreateWorldGenSpreader();
        var totalRounds = config.SpreadRounds + config.GrowthRounds;
        for (var round = 0; round < totalRounds; round++)
        {
            for (var i = 0; i < config.ChargeCount; i++) spreader.AddCursors(origin, config.AmountPerCharge);
            var spreadVeins = round < config.SpreadRounds;
            for (var i = 0; i < config.SpreadAttempts; i++)
                spreader.UpdateCursors(access, origin, random, spreadVeins);
            spreader.Clear();
        }

        var below = origin.Offset(0, -1, 0);
        var belowState = level.GetBlockState(below.X, below.Y, below.Z);
        if (random.NextFloat() <= config.CatalystChance && IsCollisionShapeFullBlock(belowState, below))
            access.SetBlock(below, Blocks.SCULK_CATALYST.DefaultBlockState);

        var extraGrowths = config.ExtraRareGrowths.Sample(random);
        for (var i = 0; i < extraGrowths; i++)
        {
            var candidate = origin.Offset(random.NextInt(5) - 2, 0, random.NextInt(5) - 2);
            if (!level.GetBlockState(candidate.X, candidate.Y, candidate.Z).Owner.IsAir) continue;
            var supportPos = candidate.Offset(0, -1, 0);
            var supportState = level.GetBlockState(supportPos.X, supportPos.Y, supportPos.Z);
            if (!MultifaceBlock.CanAttachTo(Direction.Up, supportPos, supportState)) continue;
            access.SetBlock(candidate,
                Blocks.SCULK_SHRIEKER.DefaultBlockState.SetValue(BlockStateProperties.CanSummon, true));
        }
        return true;
    }

    //CanSpreadFrom whether the cell can act as a spread origin, maps to vanilla canSpreadFrom
    //It works only from a sculk block, or from air or water that has a full solid face somewhere around it
    private static bool CanSpreadFrom(WorldGenRegion level, BlockPos origin)
    {
        var start = level.GetBlockState(origin.X, origin.Y, origin.Z);
        if (start.Owner is SculkBehaviour) return true;
        if (start.Owner.IsAir || (start.Owner == Blocks.WaterState.Owner && !start.FluidState.IsEmpty))
        {
            foreach (var direction in Direction.Values)
            {
                var pos = origin.Offset(direction);
                if (IsCollisionShapeFullBlock(level.GetBlockState(pos.X, pos.Y, pos.Z), pos)) return true;
            }
            return false;
        }
        return false;
    }

    //IsCollisionShapeFullBlock whether the state's collision shape fills the whole cell
    private static bool IsCollisionShapeFullBlock(BlockState state, BlockPos pos)
        => state.Owner is BlockBehaviour behaviour
            && behaviour.IsCollisionShapeFullBlock(state, EmptyBlockGetter.Instance, pos);
}
