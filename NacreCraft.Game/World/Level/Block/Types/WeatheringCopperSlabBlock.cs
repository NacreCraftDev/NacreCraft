using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperSlabBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperSlabBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperSlabBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperSlabBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
