using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//HorizontalDirectionalBlock block type, maps to vanilla net.minecraft.world.level.block.HorizontalDirectionalBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class HorizontalDirectionalBlock : BlockBehaviour
{
    private readonly string _name;

    public HorizontalDirectionalBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
