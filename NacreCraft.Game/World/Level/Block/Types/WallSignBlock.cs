using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WallSignBlock block type, maps to vanilla net.minecraft.world.level.block.WallSignBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WallSignBlock : BlockBehaviour
{
    private readonly string _name;

    public WallSignBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
