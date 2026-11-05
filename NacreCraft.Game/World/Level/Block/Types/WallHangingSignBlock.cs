using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WallHangingSignBlock block type, maps to vanilla net.minecraft.world.level.block.WallHangingSignBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WallHangingSignBlock : BlockBehaviour
{
    private readonly string _name;

    public WallHangingSignBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
