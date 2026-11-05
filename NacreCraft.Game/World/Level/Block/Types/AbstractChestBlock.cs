using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AbstractChestBlock block type, maps to vanilla net.minecraft.world.level.block.AbstractChestBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AbstractChestBlock : BlockBehaviour
{
    private readonly string _name;

    public AbstractChestBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
