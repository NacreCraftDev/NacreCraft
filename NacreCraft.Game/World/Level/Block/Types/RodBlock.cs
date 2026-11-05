using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//RodBlock block type, maps to vanilla net.minecraft.world.level.block.RodBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class RodBlock : BlockBehaviour
{
    private readonly string _name;

    public RodBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
