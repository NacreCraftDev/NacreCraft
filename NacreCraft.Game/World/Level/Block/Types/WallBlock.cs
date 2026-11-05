using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WallBlock block type, maps to vanilla net.minecraft.world.level.block.WallBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WallBlock : BlockBehaviour
{
    private readonly string _name;

    public WallBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
