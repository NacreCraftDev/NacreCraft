using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//RootedDirtBlock block type, maps to vanilla net.minecraft.world.level.block.RootedDirtBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class RootedDirtBlock : BlockBehaviour
{
    private readonly string _name;

    public RootedDirtBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
