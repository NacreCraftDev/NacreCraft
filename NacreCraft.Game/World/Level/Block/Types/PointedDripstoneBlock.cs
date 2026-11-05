using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PointedDripstoneBlock block type, maps to vanilla net.minecraft.world.level.block.PointedDripstoneBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PointedDripstoneBlock : BlockBehaviour
{
    private readonly string _name;

    public PointedDripstoneBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
