using NacreCraft.Game.World.Damage;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Effect;

//OozingMobEffect oozing effect, maps to vanilla OozingMobEffect
//On death the entity splits into slimes, capped by the entity cramming rule
public sealed class OozingMobEffect : MobEffect
{
    //RadiusToCheckSlimes radius searched for existing slimes, maps to vanilla RADIUS_TO_CHECK_SLIMES
    private const int RadiusToCheckSlimes = 2;

    //SlimeSize size of the spawned slimes, maps to vanilla SLIME_SIZE
    public const int SlimeSize = 2;

    //_spawnedCount requested slime count as a function of the random source, maps to vanilla ToIntFunction
    private readonly Func<RandomSource, int> _spawnedCount;

    public OozingMobEffect(MobEffectCategory category, int color, Func<RandomSource, int> spawnedCount)
        : base(category, color)
        => _spawnedCount = spawnedCount;

    //NumberOfSlimesToSpawn clamps the requested count by the room left under the cramming cap
    //maps to vanilla numberOfSlimesToSpawn, the argument order of the vanilla clamp call is preserved as is
    public static int NumberOfSlimesToSpawn(int maxEntityCramming, Func<int, int> nearbySlimes, int numberRequested)
    {
        if (maxEntityCramming < 1) return numberRequested;
        return Mth.Clamp(0, maxEntityCramming - nearbySlimes(maxEntityCramming), numberRequested);
    }

    public override void OnMobRemoved(ServerLevel level, LivingEntity mob, int amplifier, RemovalReason reason)
    {
        if (reason != RemovalReason.KILLED) return;
        var requested = _spawnedCount(mob.GetRandom());
        var maxEntityCramming = level.GameRules?.GetInt(GameRules.MaxEntityCramming) ?? 24;
        var count = NumberOfSlimesToSpawn(maxEntityCramming, max => NearbySlimeCount(mob, max), requested);
        for (var i = 0; i < count; i++)
            SpawnSlimeOffspring(level, mob.Pos.X, mob.Pos.Y + 0.5, mob.Pos.Z);
    }

    //NearbySlimeCount counts slimes around the mob, capped by the cramming limit, maps to vanilla NearbySlimes.closeTo
    private static int NearbySlimeCount(LivingEntity mob, int maxResults)
    {
        if (mob.Level is not ServerLevel level) return 0;
        var box = mob.BoundingBox.Inflate(RadiusToCheckSlimes, RadiusToCheckSlimes, RadiusToCheckSlimes);
        var slimes = level.GetEntitiesOfClass<NacreCraft.Registry.Entity>(box,
            entity => !ReferenceEquals(entity, mob) && ReferenceEquals(entity.Type, EntityTypes.SLIME));
        return Math.Min(slimes.Count, maxResults);
    }

    //SpawnSlimeOffspring puts one slime at the given position, maps to vanilla spawnSlimeOffspring
    //The Slime class is not ported, so the entity type has no factory and the spawn degrades to a no-op for now
    private static void SpawnSlimeOffspring(ServerLevel level, double x, double y, double z)
    {
        if (EntityTypes.SLIME.Create(level) is not { } slime) return;
        slime.SetPos(new Vec3(x, y, z), level.Random.NextFloat() * 360.0f, 0.0f);
        level.AddEntity(slime);
    }
}

//InfestedMobEffect infested effect, maps to vanilla InfestedMobEffect
//A fraction of hits spawns silverfish around the victim
public sealed class InfestedMobEffect : MobEffect
{
    //_chanceToSpawn probability per hit that silverfish appear, maps to vanilla chanceToSpawn
    private readonly float _chanceToSpawn;

    //_spawnedCount number of silverfish per proc as a function of the random source, maps to vanilla ToIntFunction
    private readonly Func<RandomSource, int> _spawnedCount;

    public InfestedMobEffect(MobEffectCategory category, int color, float chanceToSpawn,
        Func<RandomSource, int> spawnedCount)
        : base(category, color)
    {
        _chanceToSpawn = chanceToSpawn;
        _spawnedCount = spawnedCount;
    }

    public override void OnMobHurt(ServerLevel level, LivingEntity mob, int amplifier, DamageSource source, float damage)
    {
        var random = mob.GetRandom();
        if (random.NextFloat() > _chanceToSpawn) return;
        var count = _spawnedCount(random);
        for (var i = 0; i < count; i++)
            SpawnSilverfish(level, mob, mob.Pos.X, mob.Pos.Y + mob.Height / 2.0, mob.Pos.Z);
    }

    //SpawnSilverfish puts one silverfish next to the mob, maps to vanilla spawnSilverfish
    //The Silverfish class is not ported, so the entity type has no factory and the spawn degrades to a no-op for now;
    //the vanilla launch velocity also needs the look angle, which the entity layer does not expose yet
    private static void SpawnSilverfish(ServerLevel level, LivingEntity mob, double x, double y, double z)
    {
        if (EntityTypes.SILVERFISH.Create(level) is not { } silverfish) return;
        silverfish.SetPos(new Vec3(x, y, z), level.Random.NextFloat() * 360.0f, 0.0f);
        level.AddEntity(silverfish);
        level.PlaySound(SoundEvents.SilverfishHurt, SoundSource.Hostile, silverfish.BlockPosition(), 1.0f, 1.0f);
    }
}

//WeavingMobEffect weaving effect, maps to vanilla WeavingMobEffect
//On death the entity leaves cobwebs around the death spot, when mob griefing allows it
public sealed class WeavingMobEffect : MobEffect
{
    //_maxCobwebs requested cobweb count as a function of the random source, maps to vanilla ToIntFunction
    private readonly Func<RandomSource, int> _maxCobwebs;

    public WeavingMobEffect(MobEffectCategory category, int color, Func<RandomSource, int> maxCobwebs)
        : base(category, color)
        => _maxCobwebs = maxCobwebs;

    public override void OnMobRemoved(ServerLevel level, LivingEntity mob, int amplifier, RemovalReason reason)
    {
        if (reason != RemovalReason.KILLED) return;
        //Players always leave cobwebs; other mobs need the mob griefing rule, maps to the vanilla guard
        if (mob is Player || (level.GameRules?.GetBool(GameRules.MobGriefing) ?? true))
            SpawnCobwebsRandomlyAround(level, mob.GetRandom(), mob.BlockPosition());
    }

    //SpawnCobwebsRandomlyAround scatters up to the requested count of cobwebs in a 3x3x3 cube
    //maps to vanilla spawnCobwebsRandomlyAround; BlockPos.randomInCube is not ported, so the 15 candidate positions are drawn inline
    private void SpawnCobwebsRandomlyAround(ServerLevel level, RandomSource random, BlockPos pos)
    {
        var positionsToTransform = new HashSet<BlockPos>();
        var cobwebCount = _maxCobwebs(random);
        for (var i = 0; i < 15; i++)
        {
            var candidate = new BlockPos(
                pos.X + random.NextInt(3) - 1,
                pos.Y + random.NextInt(3) - 1,
                pos.Z + random.NextInt(3) - 1);
            if (positionsToTransform.Contains(candidate)) continue;
            if (level.GetBlockState(candidate) is not { } state || state.Owner is not BlockBehaviour behaviour
                || !behaviour.CanBeReplaced) continue;
            var belowPos = candidate.Offset(0, -1, 0);
            if (level.GetBlockState(belowPos) is not { } belowState
                || belowState.Owner is not BlockBehaviour belowBehaviour) continue;
            if (!belowBehaviour.IsFaceSturdy(level, belowPos, belowState, Direction.Up)) continue;
            positionsToTransform.Add(candidate);
            if (positionsToTransform.Count >= cobwebCount) break;
        }
        foreach (var cobwebPos in positionsToTransform)
        {
            level.SetBlock(cobwebPos, Blocks.Cobweb.DefaultBlockState, 3);
            level.LevelEvent(LevelEvent.AnimationSpawnCobweb, cobwebPos, 0);
        }
    }
}

//WindChargedMobEffect wind charged effect, maps to vanilla WindChargedMobEffect
//On death the entity bursts into a wind charge gust that pushes entities without harming them
public sealed class WindChargedMobEffect : MobEffect
{
    //WindChargeDamageCalculator wind charge blast rules: blocks are immune and entities are never damaged
    //maps to vanilla AbstractWindCharge.EXPLOSION_DAMAGE_CALCULATOR, which only leaves the knockback
    private sealed class WindChargeDamageCalculator : ExplosionDamageCalculator
    {
        //Only air passes the blast through, everything else is effectively indestructible, maps to the vanilla resistance
        public override float? GetBlockExplosionResistance(Explosion explosion, ILevelReader level, BlockPos pos,
            BlockState block, FluidState? fluid)
            => block.Owner.IsAir && (fluid is null || fluid.IsEmpty) ? null : 3_600_000.0f;

        public override bool ShouldBlockExplode(Explosion explosion, ILevelReader level, BlockPos pos,
            BlockState state, float power)
            => false;

        public override bool ShouldDamageEntity(Explosion explosion, NacreCraft.Registry.Entity entity) => false;

        public override float GetEntityDamageAmount(Explosion explosion, NacreCraft.Registry.Entity entity, float exposure)
            => 0.0f;
    }

    private static readonly ExplosionDamageCalculator WindChargeCalculator = new WindChargeDamageCalculator();

    public WindChargedMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    public override void OnMobRemoved(ServerLevel level, LivingEntity mob, int amplifier, RemovalReason reason)
    {
        if (reason != RemovalReason.KILLED) return;
        var center = new Vec3(mob.Pos.X, mob.Pos.Y + mob.Height / 2.0, mob.Pos.Z);
        var strength = 3.0f + mob.GetRandom().NextFloat() * 2.0f;
        //The gust particle lists and the dedicated breeze burst sound are not ported yet; the blast itself is what pushes entities
        new ServerExplosion(level, mob, null, WindChargeCalculator, center, strength, false, BlockInteraction.Keep)
            .Explode();
    }
}
