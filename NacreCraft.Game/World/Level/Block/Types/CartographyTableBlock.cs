using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CartographyTableBlock block type, maps to vanilla net.minecraft.world.level.block.CartographyTableBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CartographyTableBlock : BlockBehaviour
{
    private readonly string _name;

    public CartographyTableBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
