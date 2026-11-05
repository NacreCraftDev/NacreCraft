using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CrafterBlock block type, maps to vanilla net.minecraft.world.level.block.CrafterBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CrafterBlock : BlockBehaviour
{
    private readonly string _name;

    public CrafterBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
