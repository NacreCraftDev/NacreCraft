using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CeilingHangingSignBlock block type, maps to vanilla net.minecraft.world.level.block.CeilingHangingSignBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CeilingHangingSignBlock : BlockBehaviour
{
    private readonly string _name;

    public CeilingHangingSignBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
