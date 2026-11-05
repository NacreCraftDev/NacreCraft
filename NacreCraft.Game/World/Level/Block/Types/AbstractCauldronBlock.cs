using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AbstractCauldronBlock block type, maps to vanilla net.minecraft.world.level.block.AbstractCauldronBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AbstractCauldronBlock : BlockBehaviour
{
    private readonly string _name;

    public AbstractCauldronBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
