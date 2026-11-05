using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//TwistingVinesBlock block type, maps to vanilla net.minecraft.world.level.block.TwistingVinesBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class TwistingVinesBlock : BlockBehaviour
{
    private readonly string _name;

    public TwistingVinesBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
