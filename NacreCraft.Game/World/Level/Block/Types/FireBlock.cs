using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//FireBlock block type, maps to vanilla net.minecraft.world.level.block.FireBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class FireBlock : BlockBehaviour
{
    private readonly string _name;

    public FireBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
