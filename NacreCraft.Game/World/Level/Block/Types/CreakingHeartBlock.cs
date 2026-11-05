using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CreakingHeartBlock block type, maps to vanilla net.minecraft.world.level.block.CreakingHeartBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CreakingHeartBlock : BlockBehaviour
{
    private readonly string _name;

    public CreakingHeartBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
