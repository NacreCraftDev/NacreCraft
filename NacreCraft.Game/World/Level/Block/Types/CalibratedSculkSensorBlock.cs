using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//CalibratedSculkSensorBlock block type, maps to vanilla net.minecraft.world.level.block.CalibratedSculkSensorBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class CalibratedSculkSensorBlock : BlockBehaviour
{
    private readonly string _name;

    public CalibratedSculkSensorBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
