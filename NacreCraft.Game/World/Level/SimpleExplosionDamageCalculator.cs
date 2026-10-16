using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level;

//SimpleExplosionDamageCalculator a fully configurable blast, maps to vanilla net.minecraft.world.level.SimpleExplosionDamageCalculator
//Vanilla builds it from the four knobs below, which is how wind charges, beds and respawn anchors tail their own blasts
//An immune block set makes every listed block ignore the blast outright, mirroring the vanilla 3600000 resistance trick
public sealed class SimpleExplosionDamageCalculator : ExplosionDamageCalculator
{
    private readonly bool _explodesBlocks;
    private readonly bool _damagesEntities;
    private readonly float? _knockbackMultiplier;
    private readonly HolderSet<NacreCraft.Registry.Block>? _immuneBlocks;

    public SimpleExplosionDamageCalculator(bool explodesBlocks, bool damagesEntities,
        float? knockbackMultiplier, HolderSet<NacreCraft.Registry.Block>? immuneBlocks)
    {
        _explodesBlocks = explodesBlocks;
        _damagesEntities = damagesEntities;
        _knockbackMultiplier = knockbackMultiplier;
        _immuneBlocks = immuneBlocks;
    }

    //GetBlockExplosionResistance makes immune blocks survive, everything else falls back to the base calculation
    //maps to vanilla getBlockExplosionResistance
    public override float? GetBlockExplosionResistance(
        Explosion explosion, ILevelReader level, BlockPos pos, BlockState block, FluidState? fluid)
    {
        if (_immuneBlocks is not null)
            return block.Is(_immuneBlocks) ? 3600000.0f : null;
        return base.GetBlockExplosionResistance(explosion, level, pos, block, fluid);
    }

    public override bool ShouldBlockExplode(
        Explosion explosion, ILevelReader level, BlockPos pos, BlockState state, float power)
        => _explodesBlocks;

    public override bool ShouldDamageEntity(Explosion explosion, NacreCraft.Registry.Entity entity)
        => _damagesEntities;

    //GetKnockbackMultiplier a flying player rides the blast without being pushed, maps to vanilla getKnockbackMultiplier
    public override float GetKnockbackMultiplier(NacreCraft.Registry.Entity entity)
        => entity.IsFlying ? 0.0f : _knockbackMultiplier ?? base.GetKnockbackMultiplier(entity);
}
