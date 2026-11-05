using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CrossCollisionBlock block type, maps to vanilla net.minecraft.world.level.block.CrossCollisionBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CrossCollisionBlock : BlockBehaviour
{
    private readonly string _name;

    public CrossCollisionBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
