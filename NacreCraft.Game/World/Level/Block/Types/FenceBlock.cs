using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//FenceBlock block type, maps to vanilla net.minecraft.world.level.block.FenceBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class FenceBlock : BlockBehaviour
{
    private readonly string _name;

    public FenceBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
