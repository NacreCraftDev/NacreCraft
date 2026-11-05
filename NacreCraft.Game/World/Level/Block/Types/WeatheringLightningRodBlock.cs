using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringLightningRodBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringLightningRodBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringLightningRodBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringLightningRodBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
