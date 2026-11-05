using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//NetherRootsBlock block type, maps to vanilla net.minecraft.world.level.block.NetherRootsBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class NetherRootsBlock : BlockBehaviour
{
    private readonly string _name;

    public NetherRootsBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
