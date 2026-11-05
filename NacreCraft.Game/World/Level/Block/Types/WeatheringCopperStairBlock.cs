using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WeatheringCopperStairBlock block type, maps to vanilla net.minecraft.world.level.block.WeatheringCopperStairBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WeatheringCopperStairBlock : BlockBehaviour
{
    private readonly string _name;

    public WeatheringCopperStairBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
