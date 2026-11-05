using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperFullBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperFullBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperFullBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperFullBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
