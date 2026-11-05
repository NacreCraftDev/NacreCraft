using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//AnvilBlock block type, maps to vanilla net.minecraft.world.level.block.AnvilBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class AnvilBlock : BlockBehaviour
{
    private readonly string _name;

    public AnvilBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
