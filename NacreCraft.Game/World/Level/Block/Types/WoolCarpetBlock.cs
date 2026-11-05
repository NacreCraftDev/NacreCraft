using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WoolCarpetBlock block type, maps to vanilla net.minecraft.world.level.block.WoolCarpetBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WoolCarpetBlock : BlockBehaviour
{
    private readonly string _name;

    public WoolCarpetBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
