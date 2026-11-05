using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WaterloggedTransparentBlock block type, maps to vanilla net.minecraft.world.level.block.WaterloggedTransparentBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WaterloggedTransparentBlock : BlockBehaviour
{
    private readonly string _name;

    public WaterloggedTransparentBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
