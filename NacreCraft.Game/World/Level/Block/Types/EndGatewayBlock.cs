using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//EndGatewayBlock block type, maps to vanilla net.minecraft.world.level.block.EndGatewayBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class EndGatewayBlock : BlockBehaviour
{
    private readonly string _name;

    public EndGatewayBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
