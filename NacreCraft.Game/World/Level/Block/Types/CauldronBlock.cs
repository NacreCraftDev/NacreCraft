using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CauldronBlock block type, maps to vanilla net.minecraft.world.level.block.CauldronBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CauldronBlock : BlockBehaviour
{
    private readonly string _name;

    public CauldronBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
