using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CoralWallFanBlock block type, maps to vanilla net.minecraft.world.level.block.CoralWallFanBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CoralWallFanBlock : BlockBehaviour
{
    private readonly string _name;

    public CoralWallFanBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
