using NacreCraft.Codec;
using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.LevelGen.BlockPredicates;

//TrueBlockPredicate always-true predicate, maps to vanilla TrueBlockPredicate
public class TrueBlockPredicate : BlockPredicate
{
    public static readonly TrueBlockPredicate Instance = new();

    public static readonly Codec<TrueBlockPredicate> Codec = new UnitMapCodec<TrueBlockPredicate>(() => Instance);

    private TrueBlockPredicate() { }

    public override bool Test(WorldGenRegion level, BlockPos origin) => true;

    public override BlockPredicateType Type => BlockPredicateType.True;
}
