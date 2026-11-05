using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BaseCoralWallFanBlock block type, maps to vanilla net.minecraft.world.level.block.BaseCoralWallFanBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BaseCoralWallFanBlock : BlockBehaviour
{
    private readonly string _name;

    public BaseCoralWallFanBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
