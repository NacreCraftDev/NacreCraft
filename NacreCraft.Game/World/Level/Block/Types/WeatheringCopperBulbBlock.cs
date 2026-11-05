using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperBulbBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperBulbBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperBulbBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperBulbBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
