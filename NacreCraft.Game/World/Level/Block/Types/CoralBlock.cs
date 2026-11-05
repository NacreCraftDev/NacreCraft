using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CoralBlock block type, maps to vanilla net.minecraft.world.level.block.CoralBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CoralBlock : BlockBehaviour
{
    private readonly string _name;

    public CoralBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
