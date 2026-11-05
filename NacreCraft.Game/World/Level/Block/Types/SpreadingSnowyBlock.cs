using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SpreadingSnowyBlock block type, maps to vanilla net.minecraft.world.level.block.SpreadingSnowyBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SpreadingSnowyBlock : BlockBehaviour
{
    private readonly string _name;

    public SpreadingSnowyBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
