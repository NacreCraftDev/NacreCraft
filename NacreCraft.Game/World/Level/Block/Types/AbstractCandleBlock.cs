using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AbstractCandleBlock block type, maps to vanilla net.minecraft.world.level.block.AbstractCandleBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AbstractCandleBlock : BlockBehaviour
{
    private readonly string _name;

    public AbstractCandleBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
