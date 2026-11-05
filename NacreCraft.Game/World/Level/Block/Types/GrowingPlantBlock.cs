using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//GrowingPlantBlock block type, maps to vanilla net.minecraft.world.level.block.GrowingPlantBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class GrowingPlantBlock : BlockBehaviour
{
    private readonly string _name;

    public GrowingPlantBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
