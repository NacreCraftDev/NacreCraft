using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//GrowingPlantBodyBlock block type, maps to vanilla net.minecraft.world.level.block.GrowingPlantBodyBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class GrowingPlantBodyBlock : BlockBehaviour
{
    private readonly string _name;

    public GrowingPlantBodyBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
