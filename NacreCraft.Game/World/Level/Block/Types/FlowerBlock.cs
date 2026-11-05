using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//FlowerBlock block type, maps to vanilla net.minecraft.world.level.block.FlowerBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class FlowerBlock : BlockBehaviour
{
    private readonly string _name;

    public FlowerBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
