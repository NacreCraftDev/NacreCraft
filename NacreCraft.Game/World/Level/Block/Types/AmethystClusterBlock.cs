using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AmethystClusterBlock block type, maps to vanilla net.minecraft.world.level.block.AmethystClusterBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AmethystClusterBlock : BlockBehaviour
{
    private readonly string _name;

    public AmethystClusterBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
