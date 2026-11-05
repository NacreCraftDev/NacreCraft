using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperDoorBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperDoorBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperDoorBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperDoorBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
