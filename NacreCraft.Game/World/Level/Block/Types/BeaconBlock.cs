using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BeaconBlock block type, maps to vanilla net.minecraft.world.level.block.BeaconBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BeaconBlock : BlockBehaviour
{
    private readonly string _name;

    public BeaconBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
