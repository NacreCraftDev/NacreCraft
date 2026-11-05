using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//ChorusFlowerBlock block type, maps to vanilla net.minecraft.world.level.block.ChorusFlowerBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class ChorusFlowerBlock : BlockBehaviour
{
    private readonly string _name;

    public ChorusFlowerBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
