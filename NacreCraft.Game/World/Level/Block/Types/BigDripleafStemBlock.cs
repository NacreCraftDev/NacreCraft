using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BigDripleafStemBlock block type, maps to vanilla net.minecraft.world.level.block.BigDripleafStemBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BigDripleafStemBlock : BlockBehaviour
{
    private readonly string _name;

    public BigDripleafStemBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
