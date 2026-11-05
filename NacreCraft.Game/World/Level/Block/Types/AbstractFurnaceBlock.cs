using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AbstractFurnaceBlock block type, maps to vanilla net.minecraft.world.level.block.AbstractFurnaceBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AbstractFurnaceBlock : BlockBehaviour
{
    private readonly string _name;

    public AbstractFurnaceBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
