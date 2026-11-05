using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//LeavesBlock block type, maps to vanilla net.minecraft.world.level.block.LeavesBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class LeavesBlock : BlockBehaviour
{
    private readonly string _name;

    public LeavesBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
