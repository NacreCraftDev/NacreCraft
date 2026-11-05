using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//RedStoneOreBlock block type, maps to vanilla net.minecraft.world.level.block.RedStoneOreBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class RedStoneOreBlock : BlockBehaviour
{
    private readonly string _name;

    public RedStoneOreBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
