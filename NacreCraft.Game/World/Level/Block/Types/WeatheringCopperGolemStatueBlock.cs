using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperGolemStatueBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperGolemStatueBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperGolemStatueBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperGolemStatueBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
