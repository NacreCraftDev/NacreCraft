using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BellBlock block type, maps to vanilla net.minecraft.world.level.block.BellBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BellBlock : BlockBehaviour
{
    private readonly string _name;

    public BellBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
