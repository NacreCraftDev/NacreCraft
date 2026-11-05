using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SandBlock block type, maps to vanilla net.minecraft.world.level.block.SandBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SandBlock : BlockBehaviour
{
    private readonly string _name;

    public SandBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
