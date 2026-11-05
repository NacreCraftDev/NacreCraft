using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//RedStoneWireBlock block type, maps to vanilla net.minecraft.world.level.block.RedStoneWireBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class RedStoneWireBlock : BlockBehaviour
{
    private readonly string _name;

    public RedStoneWireBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
