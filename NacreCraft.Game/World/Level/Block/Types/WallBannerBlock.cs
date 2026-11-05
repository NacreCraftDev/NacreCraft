using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WallBannerBlock block type, maps to vanilla net.minecraft.world.level.block.WallBannerBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WallBannerBlock : BlockBehaviour
{
    private readonly string _name;

    public WallBannerBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
