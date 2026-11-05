using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//TestInstanceBlock block type, maps to vanilla net.minecraft.world.level.block.TestInstanceBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class TestInstanceBlock : BlockBehaviour
{
    private readonly string _name;

    public TestInstanceBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
