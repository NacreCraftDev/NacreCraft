using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//ChainBlock block type, maps to vanilla net.minecraft.world.level.block.ChainBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class ChainBlock : BlockBehaviour
{
    private readonly string _name;

    public ChainBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
