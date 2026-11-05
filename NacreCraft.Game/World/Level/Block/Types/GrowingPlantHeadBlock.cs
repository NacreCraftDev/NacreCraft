using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//GrowingPlantHeadBlock block type, maps to vanilla net.minecraft.world.level.block.GrowingPlantHeadBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class GrowingPlantHeadBlock : BlockBehaviour
{
    private readonly string _name;

    public GrowingPlantHeadBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
