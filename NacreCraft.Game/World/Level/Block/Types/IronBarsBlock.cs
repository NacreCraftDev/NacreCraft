using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level.Block;

//IronBarsBlock block type, maps to vanilla net.minecraft.world.level.block.IronBarsBlock
//Properties and states still come from the embedded block table; this class only gives the block its vanilla type
public class IronBarsBlock : BlockBehaviour
{
    private readonly string _name;

    public IronBarsBlock(string name) => _name = name;

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);
}
