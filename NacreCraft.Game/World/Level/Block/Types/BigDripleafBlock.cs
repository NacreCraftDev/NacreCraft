using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BigDripleafBlock block type, maps to vanilla net.minecraft.world.level.block.BigDripleafBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BigDripleafBlock : BlockBehaviour
{
    private readonly string _name;

    public BigDripleafBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
