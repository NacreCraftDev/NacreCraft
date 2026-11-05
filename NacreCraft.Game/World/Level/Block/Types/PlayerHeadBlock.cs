using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PlayerHeadBlock block type, maps to vanilla net.minecraft.world.level.block.PlayerHeadBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PlayerHeadBlock : BlockBehaviour
{
    private readonly string _name;

    public PlayerHeadBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
