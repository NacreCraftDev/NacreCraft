using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//VineBlock block type, maps to vanilla net.minecraft.world.level.block.VineBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class VineBlock : BlockBehaviour
{
    private readonly string _name;

    public VineBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
