using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AbstractBannerBlock block type, maps to vanilla net.minecraft.world.level.block.AbstractBannerBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AbstractBannerBlock : BlockBehaviour
{
    private readonly string _name;

    public AbstractBannerBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
