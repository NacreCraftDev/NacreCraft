using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//ConcretePowderBlock block type, maps to vanilla net.minecraft.world.level.block.ConcretePowderBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class ConcretePowderBlock : BlockBehaviour
{
    private readonly string _name;

    public ConcretePowderBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
