using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//FlowerBedBlock block type, maps to vanilla net.minecraft.world.level.block.FlowerBedBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class FlowerBedBlock : BlockBehaviour
{
    private readonly string _name;

    public FlowerBedBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
