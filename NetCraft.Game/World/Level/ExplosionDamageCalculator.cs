using NetCraft.Primitives;
using NetCraft.Registry;
using NetCraft.Registry.State;
using NetCraft.Storage;

namespace NetCraft.Game.World.Level;

//ExplosionDamageCalculator decides what an explosion does to one block or one entity
//maps to vanilla net.minecraft.world.level.ExplosionDamageCalculator
public class ExplosionDamageCalculator
{
    //GetBlockExplosionResistance how much a block resists the blast
    //Nothing there means the blast passes straight through, maps to vanilla getBlockExplosionResistance
    public virtual float? GetBlockExplosionResistance(
        Explosion explosion, ILevelReader level, BlockPos pos, BlockState block, FluidState? fluid)
    {
        var fluidResistance = fluid is null || fluid.IsEmpty ? 0.0f : fluid.Type.ExplosionResistance;
        if (block.Owner.IsAir && fluidResistance <= 0.0f) return null;
        return Math.Max(block.Owner.ExplosionResistance, fluidResistance);
    }

    //ShouldBlockExplode whether the block is destroyed by the blast, maps to vanilla shouldBlockExplode
    public virtual bool ShouldBlockExplode(Explosion explosion, ILevelReader level, BlockPos pos, BlockState state, float power)
        => true;

    //ShouldDamageEntity whether the entity takes damage from the blast, maps to vanilla shouldDamageEntity
    public virtual bool ShouldDamageEntity(Explosion explosion, NetCraft.Registry.Entity entity) => true;

    //GetKnockbackMultiplier how much the blast pushes the entity, maps to vanilla getKnockbackMultiplier
    public virtual float GetKnockbackMultiplier(NetCraft.Registry.Entity entity) => 1.0f;

    //GetEntityDamageAmount the damage dealt to one entity, maps to vanilla getEntityDamageAmount
    //Grows with exposure, which is how much of the blast the entity can actually see
    public virtual float GetEntityDamageAmount(Explosion explosion, NetCraft.Registry.Entity entity, float exposure)
    {
        var doubleRadius = explosion.Radius * 2.0f;
        var distance = entity.Pos.DistanceToSqr(explosion.Center);
        var normalized = Math.Sqrt(distance) / doubleRadius;
        var falloff = (1.0 - normalized) * exposure;
        return (float)((falloff * falloff + falloff) / 2.0 * 7.0 * doubleRadius + 1.0);
    }
}
