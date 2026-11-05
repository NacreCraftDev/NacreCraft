using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperChestBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperChestBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperChestBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperChestBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
