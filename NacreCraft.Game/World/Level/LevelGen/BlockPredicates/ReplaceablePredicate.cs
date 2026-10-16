using NacreCraft.Codec;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Level.LevelGen.BlockPredicates;

//ReplaceablePredicate replaceable check, maps to vanilla ReplaceablePredicate
public class ReplaceablePredicate : StateTestingPredicate
{
    public static readonly Codec<ReplaceablePredicate> Codec = new SingleFieldMapCodec<ReplaceablePredicate, Vec3i>(
        StateTestingCodec(), offset => new ReplaceablePredicate(offset), p => p.Offset);

    public ReplaceablePredicate(Vec3i offset) : base(offset) { }

    protected override bool Test(BlockState state)
        => state.Owner is BlockBehaviour behaviour && behaviour.CanBeReplaced;

    public override BlockPredicateType Type => BlockPredicateType.Replaceable;
}
