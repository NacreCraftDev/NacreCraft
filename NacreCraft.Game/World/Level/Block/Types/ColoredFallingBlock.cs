using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//ColoredFallingBlock block type, maps to vanilla net.minecraft.world.level.block.ColoredFallingBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class ColoredFallingBlock : BlockBehaviour
{
    private readonly string _name;

    public ColoredFallingBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
