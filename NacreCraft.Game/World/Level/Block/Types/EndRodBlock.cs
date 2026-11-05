using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//EndRodBlock block type, maps to vanilla net.minecraft.world.level.block.EndRodBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class EndRodBlock : BlockBehaviour
{
    private readonly string _name;

    public EndRodBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
