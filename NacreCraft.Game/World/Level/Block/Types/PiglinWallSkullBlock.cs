using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PiglinWallSkullBlock block type, maps to vanilla net.minecraft.world.level.block.PiglinWallSkullBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PiglinWallSkullBlock : BlockBehaviour
{
    private readonly string _name;

    public PiglinWallSkullBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
