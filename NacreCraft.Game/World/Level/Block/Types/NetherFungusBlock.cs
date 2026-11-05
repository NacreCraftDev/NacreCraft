using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//NetherFungusBlock block type, maps to vanilla net.minecraft.world.level.block.NetherFungusBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class NetherFungusBlock : BlockBehaviour
{
    private readonly string _name;

    public NetherFungusBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
