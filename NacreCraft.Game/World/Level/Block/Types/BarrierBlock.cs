using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BarrierBlock block type, maps to vanilla net.minecraft.world.level.block.BarrierBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BarrierBlock : BlockBehaviour
{
    private readonly string _name;

    public BarrierBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
