using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PlayerWallHeadBlock block type, maps to vanilla net.minecraft.world.level.block.PlayerWallHeadBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PlayerWallHeadBlock : BlockBehaviour
{
    private readonly string _name;

    public PlayerWallHeadBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
