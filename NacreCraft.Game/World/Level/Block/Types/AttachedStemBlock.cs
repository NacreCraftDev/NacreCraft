using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AttachedStemBlock block type, maps to vanilla net.minecraft.world.level.block.AttachedStemBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AttachedStemBlock : BlockBehaviour
{
    private readonly string _name;

    public AttachedStemBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
