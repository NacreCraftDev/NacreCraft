using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SpeleothemBlock block type, maps to vanilla net.minecraft.world.level.block.SpeleothemBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SpeleothemBlock : BlockBehaviour
{
    private readonly string _name;

    public SpeleothemBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
