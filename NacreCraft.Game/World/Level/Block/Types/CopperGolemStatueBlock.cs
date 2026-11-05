using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CopperGolemStatueBlock block type, maps to vanilla net.minecraft.world.level.block.CopperGolemStatueBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CopperGolemStatueBlock : BlockBehaviour
{
    private readonly string _name;

    public CopperGolemStatueBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
