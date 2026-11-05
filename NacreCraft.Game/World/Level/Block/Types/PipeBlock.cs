using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PipeBlock block type, maps to vanilla net.minecraft.world.level.block.PipeBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PipeBlock : BlockBehaviour
{
    private readonly string _name;

    public PipeBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
