using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CarvedPumpkinBlock block type, maps to vanilla net.minecraft.world.level.block.CarvedPumpkinBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CarvedPumpkinBlock : BlockBehaviour
{
    private readonly string _name;

    public CarvedPumpkinBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
