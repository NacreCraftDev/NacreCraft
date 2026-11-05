using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//HopperBlock block type, maps to vanilla net.minecraft.world.level.block.HopperBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class HopperBlock : BlockBehaviour
{
    private readonly string _name;

    public HopperBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
