using NacreCraft.Game.Server;
using NacreCraft.Game.World.Damage;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util;
using NacreCraft.Util.Collection;
using NacreCraft.Util.Random;
using NacreCraftEntity = NacreCraft.Registry.Entity;

namespace NacreCraft.Game.World.Level;

//ServerExplosion a blast running on the server, maps to vanilla net.minecraft.world.level.ServerExplosion
//Walks 16x16x16 rays outward from the centre; each step spends power on the block resistance and removes the block
//Entities in range are damaged by exposure, knocked away and told about the hit; destroyed blocks then drop loot
//The interface lives in the level package and the entity hooks in the registry, see IExplosion on the layering
public sealed class ServerExplosion : Explosion
{
    //ExplosionDamageCalculatorInstance fallback used when the blast has no source entity, maps to vanilla EXPLOSION_DAMAGE_CALCULATOR
    private static readonly ExplosionDamageCalculator ExplosionDamageCalculatorInstance = new();

    //DamageSourceFactory builds the damage source of a sourceless blast; DamageSources is not held by the level, see the constructor
    private static readonly DamageSources DamageSourceFactory = new(BuiltInRegistries.DAMAGE_TYPE);

    //MaxDropsPerCombinedStack one blast merges drops up to this count, maps to vanilla MAX_DROPS_PER_COMBINED_STACK
    private const int MaxDropsPerCombinedStack = 16;

    //LargeExplosionRadius the radius at and above which a blast is no longer small, maps to vanilla LARGE_EXPLOSION_RADIUS
    private const float LargeExplosionRadius = 2.0f;

    private readonly bool _fire;
    private readonly BlockInteraction _blockInteraction;
    private readonly ServerLevel _level;
    private readonly Vec3 _center;
    private readonly NacreCraftEntity? _source;
    private readonly float _radius;
    private readonly DamageSource _damageSource;
    private readonly ExplosionDamageCalculator _damageCalculator;
    private readonly Dictionary<Player, Vec3> _hitPlayers = new();

    //ServerExplosion builds one blast, maps to the vanilla constructor
    //A missing damage source falls back to the generic explosion source; a missing source entity falls back to the plain calculator
    public ServerExplosion(ServerLevel level, NacreCraftEntity? source, DamageSource? damageSource,
        ExplosionDamageCalculator? damageCalculator, Vec3 center, float radius, bool fire,
        BlockInteraction blockInteraction)
    {
        _level = level;
        _source = source;
        _radius = radius;
        _center = center;
        _fire = fire;
        _blockInteraction = blockInteraction;
        _damageSource = damageSource ?? DamageSourceFactory.Explosion(this);
        _damageCalculator = damageCalculator ?? MakeDamageCalculator(source);
    }

    //MakeDamageCalculator picks the calculator by whether there is a source entity, maps to vanilla makeDamageCalculator
    private static ExplosionDamageCalculator MakeDamageCalculator(NacreCraftEntity? source)
        => source is null ? ExplosionDamageCalculatorInstance : new EntityBasedExplosionDamageCalculator(source);

    //GetSeenPercent how much of the blast the entity can actually see, maps to vanilla getSeenPercent
    //Samples a grid across the entity bounding box and casts a ray from each sample to the centre; a clear ray counts as seen
    //The level is needed for the block raycast, which lives above the entity layer, so it comes in as an argument
    public static float GetSeenPercent(Vec3 center, NacreCraftEntity entity, ServerLevel level)
    {
        var box = entity.BoundingBox;
        var xs = 1.0 / ((box.Max.X - box.Min.X) * 2.0 + 1.0);
        var ys = 1.0 / ((box.Max.Y - box.Min.Y) * 2.0 + 1.0);
        var zs = 1.0 / ((box.Max.Z - box.Min.Z) * 2.0 + 1.0);
        var xOffset = (1.0 - Math.Floor(1.0 / xs) * xs) / 2.0;
        var zOffset = (1.0 - Math.Floor(1.0 / zs) * zs) / 2.0;
        if (xs < 0.0 || ys < 0.0 || zs < 0.0) return 0.0f;
        if (level is not PersistentServerLevel persistent) return 0.0f;

        var hits = 0;
        var count = 0;
        for (var xx = 0.0; xx <= 1.0; xx += xs)
        {
            for (var yy = 0.0; yy <= 1.0; yy += ys)
            {
                for (var zz = 0.0; zz <= 1.0; zz += zs)
                {
                    var from = new Vec3(
                        Mth.Lerp(xx, box.Min.X, box.Max.X) + xOffset,
                        Mth.Lerp(yy, box.Min.Y, box.Max.Y),
                        Mth.Lerp(zz, box.Min.Z, box.Max.Z) + zOffset);
                    if (ServerBlockRaycast.Clip(persistent, from, center) is null) hits++;
                    count++;
                }
            }
        }
        return (float)hits / count;
    }

    //CalculateExplodedPositions walks the rays and collects the blocks the blast reaches, maps to vanilla calculateExplodedPositions
    private List<BlockPos> CalculateExplodedPositions()
    {
        var toBlow = new HashSet<BlockPos>();
        for (var xx = 0; xx < 16; xx++)
        {
            for (var yy = 0; yy < 16; yy++)
            {
                for (var zz = 0; zz < 16; zz++)
                {
                    if (xx != 0 && xx != 15 && yy != 0 && yy != 15 && zz != 0 && zz != 15) continue;
                    var xd = xx / 15.0 * 2.0 - 1.0;
                    var yd = yy / 15.0 * 2.0 - 1.0;
                    var zd = zz / 15.0 * 2.0 - 1.0;
                    var length = Math.Sqrt(xd * xd + yd * yd + zd * zd);
                    var xd2 = xd / length;
                    var yd2 = yd / length;
                    var zd2 = zd / length;
                    //The starting power is jittered per ray, which is what makes two identical blasts differ, maps to vanilla nextFloat
                    var remainingPower = _radius * (0.7f + _level.Random.NextFloat() * 0.6f);
                    double xp = _center.X;
                    double yp = _center.Y;
                    double zp = _center.Z;
                    while (remainingPower > 0.0f)
                    {
                        var pos = new BlockPos(Mth.Floor(xp), Mth.Floor(yp), Mth.Floor(zp));
                        var block = _level.GetBlockState(pos) ?? AirState;
                        var fluid = _level.GetFluidState(pos);
                        if (!IsInWorldBounds(pos)) break;
                        var resistance = _damageCalculator.GetBlockExplosionResistance(this, _level, pos, block, fluid);
                        if (resistance is { } value) remainingPower -= (value + 0.3f) * 0.3f;
                        if (remainingPower > 0.0f
                            && _damageCalculator.ShouldBlockExplode(this, _level, pos, block, remainingPower))
                            toBlow.Add(pos);
                        xp += xd2 * 0.3;
                        yp += yd2 * 0.3;
                        zp += zd2 * 0.3;
                        remainingPower -= 0.225f;
                    }
                }
            }
        }
        return new List<BlockPos>(toBlow);
    }

    //HurtEntities damages and knocks back every entity the blast reaches, maps to vanilla hurtEntities
    private void HurtEntities()
    {
        if (_radius < 1.0E-5f) return;
        var doubleRadius = _radius * 2.0f;
        var box = new AABB(
            Mth.Floor(_center.X - doubleRadius - 1.0), Mth.Floor(_center.Y - doubleRadius - 1.0),
            Mth.Floor(_center.Z - doubleRadius - 1.0), Mth.Floor(_center.X + doubleRadius + 1.0),
            Mth.Floor(_center.Y + doubleRadius + 1.0), Mth.Floor(_center.Z + doubleRadius + 1.0));

        foreach (var entity in _level.EntitiesInBox(box))
        {
            if (entity.IgnoreExplosion(this)) continue;
            var distance = Math.Sqrt(entity.Pos.DistanceToSqr(_center)) / doubleRadius;
            if (distance > 1.0) continue;

            //Primed TNT would measure from its position instead of its eyes; that entity type does not exist here yet
            var origin = entity.GetEyePosition();
            var direction = origin.Subtract(_center).Normalize();
            var shouldDamage = _damageCalculator.ShouldDamageEntity(this, entity);
            var knockbackMultiplier = _damageCalculator.GetKnockbackMultiplier(entity);
            var exposure = shouldDamage || knockbackMultiplier != 0.0f
                ? GetSeenPercent(_center, entity, _level)
                : 0.0f;
            if (shouldDamage) DamageEntity(entity, exposure);

            var knockbackResistance = entity is LivingEntity living
                ? living.GetAttributeValue(NacreCraft.Registry.EntityAttribute.Attributes.ExplosionKnockbackResistance)
                : 0.0;
            var knockbackPower = (1.0 - distance) * exposure * knockbackMultiplier * (1.0 - knockbackResistance);
            var knockback = direction.Multiply(knockbackPower);
            entity.Push(knockback);

            //The redirectable projectile tag is not wired up, so every projectile takes the new owner; a blasting projectile
            //is exactly the case the vanilla tag is there for, and non-redirectable ones simply keep the owner they had
            if (entity is Projectile projectile) projectile.SetOwner(_damageSource.CausingEntity);
            else if (entity is Player player && ShouldRecordHit(player))
                _hitPlayers[player] = knockback;

            entity.OnExplosionHit(_source);
        }
    }

    //ShouldRecordHit whether the blast knockback is worth recording for this player, maps to the vanilla player branch
    //Spectators and creative players in flight keep their own movement; the recorded impulse is replayed by the client
    private static bool ShouldRecordHit(Player player)
    {
        var type = GameType.ById(player.GameMode);
        return type != GameType.Spectator && !(type == GameType.Creative && player.IsFlying);
    }

    //DamageEntity applies the blast damage, maps to vanilla hurtServer
    //Only living entities take damage; the base entity hook is a no-op
    private void DamageEntity(NacreCraftEntity entity, float exposure)
    {
        if (entity is LivingEntity living)
            living.Hurt(_damageCalculator.GetEntityDamageAmount(this, entity, exposure), _damageSource);
    }

    //InteractWithBlocks turns the destroyed blocks into drops, maps to vanilla interactWithBlocks
    //The order is shuffled so the merge target is not always the lowest coordinate
    private void InteractWithBlocks(List<BlockPos> targetBlocks)
    {
        var stacks = new List<StackCollector>();
        RandomCollections.Shuffle(targetBlocks, _level.Random);
        foreach (var pos in targetBlocks)
        {
            if (_level.GetBlockState(pos) is not { } block) continue;
            if (block.Owner is not NacreCraft.Game.World.Level.Block.BlockBehaviour behaviour) continue;
            behaviour.OnExplosionHit(block, _level, pos, this,
                (stack, position) => AddOrAppendStack(stacks, stack, position));
        }
        foreach (var collector in stacks)
            NacreCraft.Game.World.Level.Block.BlockBehaviour.PopResource(_level, collector.Pos, collector.Stack);
    }

    //CreateFire lights a fire on exposed blocks above solid ground, maps to vanilla createFire
    private void CreateFire(List<BlockPos> targetBlocks)
    {
        foreach (var pos in targetBlocks)
        {
            if (_level.Random.NextInt(3) != 0) continue;
            if (_level.GetBlockState(pos) is not { } block || !block.Owner.IsAir) continue;
            if (_level.GetBlockState(pos.Offset(0, -1, 0)) is not { } below || !below.Owner.SolidRender(below)) continue;
            _level.SetBlock(pos, NacreCraft.Game.World.Level.Block.Blocks.BaseFireBlock.GetState(_level, pos));
        }
    }

    //AddOrAppendStack merges a drop into an existing stack when possible, maps to vanilla addOrAppendStack
    private static void AddOrAppendStack(List<StackCollector> stacks, ItemStack stack, BlockPos pos)
    {
        foreach (var collector in stacks)
        {
            collector.TryMerge(stack);
            if (stack.IsEmpty()) return;
        }
        stacks.Add(new StackCollector(pos, stack));
    }

    //Explode runs the whole blast and returns how many blocks were destroyed, maps to vanilla explode
    public int Explode()
    {
        _level.GameEvent(_source, GameEvent.Explode, _center);
        var toBlow = CalculateExplodedPositions();
        HurtEntities();
        if (InteractsWithBlocks()) InteractWithBlocks(toBlow);
        if (_fire) CreateFire(toBlow);
        return toBlow.Count;
    }

    //InteractsWithBlocks whether the blast does anything to blocks, maps to vanilla interactsWithBlocks
    public bool InteractsWithBlocks() => _blockInteraction != BlockInteraction.Keep;

    //IsInWorldBounds whether the position is inside the build limits, maps to vanilla isInWorldBounds
    //The entity system has no block getter for this, so the build height fields on the level are used directly
    private bool IsInWorldBounds(BlockPos pos)
        => pos.Y >= _level.MinBuildHeight && pos.Y < _level.MaxBuildHeight;

    //AirState stands in for an unloaded cell; the vanilla getBlockState never returns null, a missing chunk here does
    private static BlockState AirState => NacreCraft.Game.World.Level.Block.Blocks.AIR.DefaultBlockState;

    //IsSmall whether the blast is a small one, maps to vanilla isSmall
    public bool IsSmall() => _radius < LargeExplosionRadius || !InteractsWithBlocks();

    //HitPlayers the knockback handed to each affected player, replayed by the client, maps to vanilla getHitPlayers
    public IReadOnlyDictionary<Player, Vec3> HitPlayers => _hitPlayers;

    //DamageSource the damage source of this blast, maps to vanilla getDamageSource
    public DamageSource DamageSource => _damageSource;

    public ServerLevel Level => _level;

    public BlockInteraction Interaction => _blockInteraction;

    public LivingEntity? IndirectSourceEntity => ExplosionSupport.GetIndirectSourceEntity(_source);

    public NacreCraftEntity? DirectSourceEntity => _source;

    public float Radius => _radius;

    public Vec3 Center => _center;

    //CanTriggerBlocks whether the blast may set off blocks like TNT
    //Vanilla also checks mob griefing for breeze wind charges; that entity type does not exist here yet
    public bool CanTriggerBlocks => _blockInteraction == BlockInteraction.TriggerBlock;

    //ShouldAffectBlocklikeEntities whether blocks that behave like entities are caught
    //Vanilla returns true outright while mob griefing is on and only consults the interaction mode once it is off;
    //the level carries no game rule entry yet, so the interaction mode alone decides this for now
    public bool ShouldAffectBlocklikeEntities => _blockInteraction.ShouldAffectBlocklikeEntities();

    //StackCollector merges the drops of one blast into as few stacks as possible, maps to vanilla StackCollector
    private sealed class StackCollector(BlockPos pos, ItemStack stack)
    {
        public BlockPos Pos { get; } = pos;

        public ItemStack Stack { get; private set; } = stack;

        //TryMerge folds input into the held stack, maps to vanilla tryMerge
        public void TryMerge(ItemStack input)
        {
            if (ItemEntity.AreMergable(Stack, input))
                Stack = ItemEntity.Merge(Stack, input, MaxDropsPerCombinedStack);
        }
    }
}
