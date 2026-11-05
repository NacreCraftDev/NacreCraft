using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WitherWallSkullBlock block type, maps to vanilla net.minecraft.world.level.block.WitherWallSkullBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WitherWallSkullBlock : BlockBehaviour
{
    private readonly string _name;

    public WitherWallSkullBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
