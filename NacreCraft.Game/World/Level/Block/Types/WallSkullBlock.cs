using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WallSkullBlock block type, maps to vanilla net.minecraft.world.level.block.WallSkullBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WallSkullBlock : BlockBehaviour
{
    private readonly string _name;

    public WallSkullBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
