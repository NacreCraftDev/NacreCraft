using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PumpkinBlock block type, maps to vanilla net.minecraft.world.level.block.PumpkinBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PumpkinBlock : BlockBehaviour
{
    private readonly string _name;

    public PumpkinBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
