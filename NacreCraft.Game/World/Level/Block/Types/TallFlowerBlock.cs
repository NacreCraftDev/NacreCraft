using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//TallFlowerBlock block type, maps to vanilla net.minecraft.world.level.block.TallFlowerBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class TallFlowerBlock : BlockBehaviour
{
    private readonly string _name;

    public TallFlowerBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
