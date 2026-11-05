using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//DoublePlantBlock block type, maps to vanilla net.minecraft.world.level.block.DoublePlantBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class DoublePlantBlock : BlockBehaviour
{
    private readonly string _name;

    public DoublePlantBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
