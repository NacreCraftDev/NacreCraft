using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//WitherRoseBlock block type, maps to vanilla net.minecraft.world.level.block.WitherRoseBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class WitherRoseBlock : BlockBehaviour
{
    private readonly string _name;

    public WitherRoseBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
