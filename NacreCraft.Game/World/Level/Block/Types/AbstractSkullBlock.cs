using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AbstractSkullBlock block type, maps to vanilla net.minecraft.world.level.block.AbstractSkullBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AbstractSkullBlock : BlockBehaviour
{
    private readonly string _name;

    public AbstractSkullBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
