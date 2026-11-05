using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//BedBlock block type, maps to vanilla net.minecraft.world.level.block.BedBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class BedBlock : BlockBehaviour
{
    private readonly string _name;

    public BedBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
