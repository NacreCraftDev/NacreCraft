using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WallTorchBlock block type, maps to vanilla net.minecraft.world.level.block.WallTorchBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WallTorchBlock : BlockBehaviour
{
    private readonly string _name;

    public WallTorchBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
