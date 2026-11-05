using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PowderSnowBlock block type, maps to vanilla net.minecraft.world.level.block.PowderSnowBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PowderSnowBlock : BlockBehaviour
{
    private readonly string _name;

    public PowderSnowBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
