using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//TwistingVinesPlantBlock block type, maps to vanilla net.minecraft.world.level.block.TwistingVinesPlantBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class TwistingVinesPlantBlock : BlockBehaviour
{
    private readonly string _name;

    public TwistingVinesPlantBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
