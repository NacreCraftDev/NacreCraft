using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//LeafLitterBlock block type, maps to vanilla net.minecraft.world.level.block.LeafLitterBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class LeafLitterBlock : BlockBehaviour
{
    private readonly string _name;

    public LeafLitterBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
