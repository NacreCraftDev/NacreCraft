using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperGrateBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperGrateBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperGrateBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperGrateBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
