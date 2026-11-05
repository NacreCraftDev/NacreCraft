using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//NetherrackBlock block type, maps to vanilla net.minecraft.world.level.block.NetherrackBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class NetherrackBlock : BlockBehaviour
{
    private readonly string _name;

    public NetherrackBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
