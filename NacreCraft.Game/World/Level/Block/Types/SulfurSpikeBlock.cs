using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//SulfurSpikeBlock block type, maps to vanilla net.minecraft.world.level.block.SulfurSpikeBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class SulfurSpikeBlock : BlockBehaviour
{
    private readonly string _name;

    public SulfurSpikeBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
