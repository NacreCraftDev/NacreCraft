using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//MagmaBlock block type, maps to vanilla net.minecraft.world.level.block.MagmaBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class MagmaBlock : BlockBehaviour
{
    private readonly string _name;

    public MagmaBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
