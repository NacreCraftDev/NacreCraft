using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//TrappedChestBlock block type, maps to vanilla net.minecraft.world.level.block.TrappedChestBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class TrappedChestBlock : BlockBehaviour
{
    private readonly string _name;

    public TrappedChestBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
