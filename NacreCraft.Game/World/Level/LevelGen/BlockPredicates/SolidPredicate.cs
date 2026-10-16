using NacreCraft.Codec;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Level.LevelGen.BlockPredicates;

//SolidPredicate solid check, maps to vanilla SolidPredicate
public class SolidPredicate : StateTestingPredicate
{
    public static readonly Codec<SolidPredicate> Codec = new SingleFieldMapCodec<SolidPredicate, Vec3i>(
        StateTestingCodec(), offset => new SolidPredicate(offset), p => p.Offset);

    public SolidPredicate(Vec3i offset) : base(offset) { }

    //NacreCraft has no material.isSolid yet, so light attenuation approximates an opaque solid
    protected override bool Test(BlockState state) => state.GetLightDampening() >= 15;

    public override BlockPredicateType Type => BlockPredicateType.Solid;
}
