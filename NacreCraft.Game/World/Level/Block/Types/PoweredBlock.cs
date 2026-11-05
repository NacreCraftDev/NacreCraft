using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//PoweredBlock block type, maps to vanilla net.minecraft.world.level.block.PoweredBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class PoweredBlock : BlockBehaviour
{
    private readonly string _name;

    public PoweredBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
