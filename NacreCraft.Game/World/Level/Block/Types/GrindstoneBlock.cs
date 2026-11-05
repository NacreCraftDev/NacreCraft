using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//GrindstoneBlock block type, maps to vanilla net.minecraft.world.level.block.GrindstoneBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class GrindstoneBlock : BlockBehaviour
{
    private readonly string _name;

    public GrindstoneBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
