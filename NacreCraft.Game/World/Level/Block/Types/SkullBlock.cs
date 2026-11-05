using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SkullBlock block type, maps to vanilla net.minecraft.world.level.block.SkullBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SkullBlock : BlockBehaviour
{
    private readonly string _name;

    public SkullBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
