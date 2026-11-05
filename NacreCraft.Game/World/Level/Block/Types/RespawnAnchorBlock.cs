using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//RespawnAnchorBlock block type, maps to vanilla net.minecraft.world.level.block.RespawnAnchorBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class RespawnAnchorBlock : BlockBehaviour
{
    private readonly string _name;

    public RespawnAnchorBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
