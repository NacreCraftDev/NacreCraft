using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CopperChestBlock block type, maps to vanilla net.minecraft.world.level.block.CopperChestBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CopperChestBlock : BlockBehaviour
{
    private readonly string _name;

    public CopperChestBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
