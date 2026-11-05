using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//MyceliumBlock block type, maps to vanilla net.minecraft.world.level.block.MyceliumBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class MyceliumBlock : BlockBehaviour
{
    private readonly string _name;

    public MyceliumBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
