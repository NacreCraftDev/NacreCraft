using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperBarsBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperBarsBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperBarsBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperBarsBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
