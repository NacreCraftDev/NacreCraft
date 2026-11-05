using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//NetherPortalBlock block type, maps to vanilla net.minecraft.world.level.block.NetherPortalBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class NetherPortalBlock : BlockBehaviour
{
    private readonly string _name;

    public NetherPortalBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
