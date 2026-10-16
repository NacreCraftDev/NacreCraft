using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level;

//EntityBasedExplosionDamageCalculator delegates the blast decisions to the source entity
//maps to vanilla net.minecraft.world.level.EntityBasedExplosionDamageCalculator
//The base class decides the raw block resistance; the source entity may raise or veto it, which is how a wither skull
//blasts through obsidian and a minecart with TNT spares the rails underneath
public sealed class EntityBasedExplosionDamageCalculator : ExplosionDamageCalculator
{
    private readonly NacreCraft.Registry.Entity _source;

    public EntityBasedExplosionDamageCalculator(NacreCraft.Registry.Entity source) => _source = source;

    //GetBlockExplosionResistance lets the source raise the resistance of a block, maps to vanilla getBlockExplosionResistance
    //Nothing there means the blast passes straight through and stays that way
    public override float? GetBlockExplosionResistance(
        Explosion explosion, ILevelReader level, BlockPos pos, BlockState block, FluidState? fluid)
    {
        var resistance = base.GetBlockExplosionResistance(explosion, level, pos, block, fluid);
        return resistance is { } value
            ? _source.GetBlockExplosionResistance(explosion, level, pos, block, fluid, value)
            : null;
    }

    //ShouldBlockExplode lets the source veto destroying the block, maps to vanilla shouldBlockExplode
    public override bool ShouldBlockExplode(
        Explosion explosion, ILevelReader level, BlockPos pos, BlockState state, float power)
        => _source.ShouldBlockExplode(explosion, level, pos, state, power);
}
