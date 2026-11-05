using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WetSpongeBlock block type, maps to vanilla net.minecraft.world.level.block.WetSpongeBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WetSpongeBlock : BlockBehaviour
{
    private readonly string _name;

    public WetSpongeBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
