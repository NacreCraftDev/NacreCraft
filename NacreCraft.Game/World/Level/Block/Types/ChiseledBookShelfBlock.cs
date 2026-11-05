using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//ChiseledBookShelfBlock block type, maps to vanilla net.minecraft.world.level.block.ChiseledBookShelfBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class ChiseledBookShelfBlock : BlockBehaviour
{
    private readonly string _name;

    public ChiseledBookShelfBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
