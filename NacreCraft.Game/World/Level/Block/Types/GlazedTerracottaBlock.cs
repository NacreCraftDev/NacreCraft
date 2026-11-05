using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//GlazedTerracottaBlock block type, maps to vanilla net.minecraft.world.level.block.GlazedTerracottaBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class GlazedTerracottaBlock : BlockBehaviour
{
    private readonly string _name;

    public GlazedTerracottaBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
